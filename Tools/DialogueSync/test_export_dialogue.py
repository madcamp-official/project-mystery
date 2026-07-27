from __future__ import annotations

import csv
import importlib.util
import tempfile
import unittest
import zipfile
from pathlib import Path

MODULE_PATH = Path(__file__).with_name("export_dialogue.py")
SPEC = importlib.util.spec_from_file_location("export_dialogue", MODULE_PATH)
SYNC = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(SYNC)


class DialogueSyncTests(unittest.TestCase):
    def test_column_index_supports_single_and_multiple_letters(self) -> None:
        self.assertEqual(0, SYNC.column_index("A1"))
        self.assertEqual(14, SYNC.column_index("O1067"))
        self.assertEqual(26, SYNC.column_index("AA4"))

    def test_extract_table_skips_title_and_blank_rows(self) -> None:
        headers, rows = SYNC.extract_table(
            [
                ["Dialogue Master"],
                [],
                ["line_id", "scene_id", "text_ko"],
                ["P-01_001", "P-01", "첫 대사"],
                ["", "", ""],
                ["P-01_002", "P-01", "둘째 대사"],
            ],
            "line_id",
        )
        self.assertEqual(["line_id", "scene_id", "text_ko"], headers)
        self.assertEqual(2, len(rows))
        self.assertEqual("둘째 대사", rows[1][2])

    def test_validate_unique_rejects_duplicates(self) -> None:
        with self.assertRaisesRegex(ValueError, "중복"):
            SYNC.validate_unique(
                "Choice_Flow",
                ["choice_id", "scene_id"],
                [["P-01_C1", "P-01"], ["P-01_C1", "P-01"]],
                "choice_id",
            )

    def test_write_csv_uses_utf8_bom_and_lf(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "dialogue.csv"
            SYNC.write_csv(
                output,
                ["line_id", "text_ko"],
                [["P-01_001", "쉼표, 따옴표 \"확인\""]],
            )
            payload = output.read_bytes()
            self.assertTrue(payload.startswith(b"\xef\xbb\xbf"))
            self.assertNotIn(b"\r\n", payload)
            with output.open(encoding="utf-8-sig", newline="") as stream:
                rows = list(csv.reader(stream))
            self.assertEqual("쉼표, 따옴표 \"확인\"", rows[1][1])

    def test_shared_and_inline_strings_are_read(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            workbook = Path(directory) / "sample.xlsx"
            self._write_sample_workbook(workbook)
            rows = SYNC.read_sheet(workbook, "Dialogue_Master")
            self.assertEqual("line_id", rows[0][0])
            self.assertEqual("P-01_001", rows[1][0])
            self.assertEqual("한국어 대사", rows[1][2])

    @staticmethod
    def _write_sample_workbook(path: Path) -> None:
        workbook = """<?xml version="1.0" encoding="UTF-8"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
 xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
 <sheets><sheet name="Dialogue_Master" sheetId="1" r:id="rId1"/></sheets>
</workbook>"""
        relationships = """<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
 <Relationship Id="rId1" Target="worksheets/sheet1.xml"
  Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"/>
</Relationships>"""
        strings = """<?xml version="1.0" encoding="UTF-8"?>
<sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
 <si><t>line_id</t></si><si><t>scene_id</t></si><si><t>text_ko</t></si>
 <si><t>P-01_001</t></si><si><t>P-01</t></si>
</sst>"""
        sheet = """<?xml version="1.0" encoding="UTF-8"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
 <sheetData>
  <row r="1"><c r="A1" t="s"><v>0</v></c><c r="B1" t="s"><v>1</v></c>
   <c r="C1" t="s"><v>2</v></c></row>
  <row r="2"><c r="A2" t="s"><v>3</v></c><c r="B2" t="s"><v>4</v></c>
   <c r="C2" t="inlineStr"><is><t>한국어 대사</t></is></c></row>
 </sheetData>
</worksheet>"""
        with zipfile.ZipFile(path, "w") as archive:
            archive.writestr("xl/workbook.xml", workbook)
            archive.writestr("xl/_rels/workbook.xml.rels", relationships)
            archive.writestr("xl/sharedStrings.xml", strings)
            archive.writestr("xl/worksheets/sheet1.xml", sheet)


if __name__ == "__main__":
    unittest.main()
