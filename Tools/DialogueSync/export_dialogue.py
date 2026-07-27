from __future__ import annotations

import csv
import hashlib
import json
import posixpath
import re
import sys
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET

MAIN_NS = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
DOC_REL_NS = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
PKG_REL_NS = "http://schemas.openxmlformats.org/package/2006/relationships"
XML_NS = "http://www.w3.org/XML/1998/namespace"
CELL_REF = re.compile(r"(?P<column>[A-Z]+)(?P<row>[0-9]+)")

TABLES = {
    "Dialogue_Master": {
        "output": "Under_the_Horizon_Dialogue_KR.csv",
        "key": "line_id",
        "expected": "dialogueRows",
    },
    "Choice_Flow": {
        "output": "Under_the_Horizon_Choices_KR.csv",
        "key": "choice_id",
        "expected": "choices",
    },
    "Scene_Index": {
        "output": "Under_the_Horizon_Scene_Index_KR.csv",
        "key": "scene_id",
        "expected": "scenes",
    },
}


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def column_index(reference: str) -> int:
    match = CELL_REF.fullmatch(reference)
    if match is None:
        raise ValueError(f"잘못된 셀 주소입니다: {reference}")
    result = 0
    for character in match.group("column"):
        result = result * 26 + ord(character) - ord("A") + 1
    return result - 1


def relationship_target(archive: zipfile.ZipFile, sheet_name: str) -> str:
    workbook = ET.fromstring(archive.read("xl/workbook.xml"))
    relationship_id = None
    for sheet in workbook.findall(f".//{{{MAIN_NS}}}sheet"):
        if sheet.attrib.get("name") == sheet_name:
            relationship_id = sheet.attrib.get(f"{{{DOC_REL_NS}}}id")
            break
    if relationship_id is None:
        raise ValueError(f"XLSX 시트를 찾을 수 없습니다: {sheet_name}")

    relationships = ET.fromstring(
        archive.read("xl/_rels/workbook.xml.rels")
    )
    for relationship in relationships.findall(f"{{{PKG_REL_NS}}}Relationship"):
        if relationship.attrib.get("Id") != relationship_id:
            continue
        target = relationship.attrib["Target"].replace("\\", "/")
        if target.startswith("/"):
            return target.lstrip("/")
        return posixpath.normpath(posixpath.join("xl", target))
    raise ValueError(f"시트 관계를 찾을 수 없습니다: {sheet_name}")


def shared_strings(archive: zipfile.ZipFile) -> list[str]:
    if "xl/sharedStrings.xml" not in archive.namelist():
        return []
    root = ET.fromstring(archive.read("xl/sharedStrings.xml"))
    values = []
    for item in root.findall(f"{{{MAIN_NS}}}si"):
        values.append(
            "".join(node.text or "" for node in item.iter(f"{{{MAIN_NS}}}t"))
        )
    return values


def cell_value(cell: ET.Element, strings: list[str]) -> str:
    cell_type = cell.attrib.get("t", "")
    if cell_type == "inlineStr":
        return "".join(
            node.text or "" for node in cell.iter(f"{{{MAIN_NS}}}t")
        )
    value_node = cell.find(f"{{{MAIN_NS}}}v")
    value = "" if value_node is None else value_node.text or ""
    if cell_type == "s" and value:
        return strings[int(value)]
    if cell_type == "b":
        return "TRUE" if value == "1" else "FALSE"
    if cell_type in {"str", "e"}:
        return value
    if re.fullmatch(r"-?[0-9]+(?:\.0+)?", value):
        return value.split(".", 1)[0]
    return value


def read_sheet(path: Path, sheet_name: str) -> list[list[str]]:
    with zipfile.ZipFile(path) as archive:
        strings = shared_strings(archive)
        root = ET.fromstring(archive.read(relationship_target(archive, sheet_name)))
        rows: list[list[str]] = []
        for row in root.findall(f".//{{{MAIN_NS}}}sheetData/{{{MAIN_NS}}}row"):
            values: list[str] = []
            for cell in row.findall(f"{{{MAIN_NS}}}c"):
                index = column_index(cell.attrib["r"])
                while len(values) <= index:
                    values.append("")
                values[index] = cell_value(cell, strings)
            rows.append(values)
        return rows


def extract_table(rows: list[list[str]], key: str) -> tuple[list[str], list[list[str]]]:
    header_index = next(
        (index for index, row in enumerate(rows) if row and row[0].strip() == key),
        None,
    )
    if header_index is None:
        raise ValueError(f"헤더 행을 찾을 수 없습니다: {key}")
    headers = [value.strip() for value in rows[header_index]]
    data: list[list[str]] = []
    for source in rows[header_index + 1 :]:
        normalized = (source + [""] * len(headers))[: len(headers)]
        if any(value.strip() for value in normalized):
            data.append(normalized)
    return headers, data


def validate_unique(
    sheet_name: str, headers: list[str], rows: list[list[str]], key: str
) -> set[str]:
    try:
        key_index = headers.index(key)
    except ValueError as error:
        raise ValueError(f"{sheet_name}에 {key} 열이 없습니다.") from error
    values = [row[key_index].strip() for row in rows]
    if any(not value for value in values):
        raise ValueError(f"{sheet_name}에 빈 {key}가 있습니다.")
    duplicates = sorted({value for value in values if values.count(value) > 1})
    if duplicates:
        raise ValueError(f"{sheet_name}의 {key}가 중복됩니다: {duplicates[:5]}")
    return set(values)


def write_csv(path: Path, headers: list[str], rows: list[list[str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(f"{path.suffix}.tmp")
    with temporary.open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.writer(stream, lineterminator="\n")
        writer.writerow(headers)
        writer.writerows(rows)
    temporary.replace(path)


def load_contract(repo_root: Path) -> tuple[dict, Path]:
    manifest_path = repo_root / "Documentation/Source/sources.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    workbook_entry = next(
        item for item in manifest["files"] if item["name"].endswith(".xlsx")
    )
    workbook_path = manifest_path.parent / workbook_entry["name"]
    actual_hash = sha256(workbook_path)
    if actual_hash != workbook_entry["sha256"]:
        raise ValueError(
            "공식 XLSX의 SHA-256이 sources.json과 다릅니다: "
            f"{actual_hash}"
        )
    return manifest["dialogueContract"], workbook_path


def export(repo_root: Path) -> dict[str, int]:
    contract, workbook_path = load_contract(repo_root)
    output_dir = repo_root / "Assets/_Project/Content/Dialogue"
    extracted: dict[str, tuple[list[str], list[list[str]]]] = {}
    keys: dict[str, set[str]] = {}

    for sheet_name, config in TABLES.items():
        headers, rows = extract_table(
            read_sheet(workbook_path, sheet_name), config["key"]
        )
        expected = int(contract[config["expected"]])
        if len(rows) != expected:
            raise ValueError(
                f"{sheet_name} 행 수 불일치: 기대 {expected}, 실제 {len(rows)}"
            )
        extracted[sheet_name] = (headers, rows)
        keys[sheet_name] = validate_unique(
            sheet_name, headers, rows, config["key"]
        )

    scene_ids = keys["Scene_Index"]
    for sheet_name in ("Dialogue_Master", "Choice_Flow"):
        headers, rows = extracted[sheet_name]
        scene_index = headers.index("scene_id")
        unknown = sorted(
            {row[scene_index].strip() for row in rows} - scene_ids
        )
        if unknown:
            raise ValueError(
                f"{sheet_name}에 미등록 scene_id가 있습니다: {unknown}"
            )

    for sheet_name, config in TABLES.items():
        headers, rows = extracted[sheet_name]
        write_csv(output_dir / config["output"], headers, rows)

    return {sheet: len(rows) for sheet, (_, rows) in extracted.items()}


def main() -> int:
    repo_root = Path(__file__).resolve().parents[2]
    try:
        report = export(repo_root)
    except (KeyError, OSError, ValueError, zipfile.BadZipFile) as error:
        print(f"대사집 동기화 실패: {error}", file=sys.stderr)
        return 1
    print(
        "대사집 동기화 완료: "
        + ", ".join(f"{sheet}={count}" for sheet, count in report.items())
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
