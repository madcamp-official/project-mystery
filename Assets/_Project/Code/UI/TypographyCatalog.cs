using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Wake.UI
{
    [CreateAssetMenu(
        fileName = "TypographyCatalog",
        menuName = "Wake/UI/Typography Catalog")]
    public sealed class TypographyCatalog : ScriptableObject
    {
        [Header("Pretendard")]
        [SerializeField] private TMP_FontAsset body;
        [SerializeField] private TMP_FontAsset bodyRegular;
        [SerializeField] private TMP_FontAsset choice;

        [Header("SUITE")]
        [SerializeField] private TMP_FontAsset speakerName;
        [SerializeField] private TMP_FontAsset heading;
        [SerializeField] private TMP_FontAsset headingStrong;

        [Header("IBM Plex Mono")]
        [SerializeField] private TMP_FontAsset technical;
        [SerializeField] private TMP_FontAsset technicalStrong;

        [Header("Special use")]
        [SerializeField] private TMP_FontAsset handwritten;
        [SerializeField] private TMP_FontAsset specialAlert;
        [SerializeField] private TMP_FontAsset specialComic;

        public TMP_FontAsset Body => Resolve(TypographyRole.Body);

        public TMP_FontAsset Resolve(TypographyRole role)
        {
            TMP_FontAsset selected = role switch
            {
                TypographyRole.Body => body,
                TypographyRole.BodyRegular => bodyRegular,
                TypographyRole.Choice => choice,
                TypographyRole.SpeakerName => speakerName,
                TypographyRole.Heading => heading,
                TypographyRole.HeadingStrong => headingStrong,
                TypographyRole.Technical => technical,
                TypographyRole.TechnicalStrong => technicalStrong,
                TypographyRole.Handwritten => handwritten,
                TypographyRole.SpecialAlert => specialAlert,
                TypographyRole.SpecialComic => specialComic,
                _ => null
            };

            if (selected != null)
            {
                return selected;
            }

            if (role != TypographyRole.Body && body != null)
            {
                return body;
            }

            return TMP_Settings.defaultFontAsset;
        }

        public IReadOnlyList<TypographyRole> GetMissingRoles(
            bool includeSpecialRoles = false)
        {
            List<TypographyRole> missing = new();
            AddIfMissing(missing, TypographyRole.Body, body);
            AddIfMissing(missing, TypographyRole.BodyRegular, bodyRegular);
            AddIfMissing(missing, TypographyRole.Choice, choice);
            AddIfMissing(missing, TypographyRole.SpeakerName, speakerName);
            AddIfMissing(missing, TypographyRole.Heading, heading);
            AddIfMissing(
                missing,
                TypographyRole.HeadingStrong,
                headingStrong);
            AddIfMissing(missing, TypographyRole.Technical, technical);
            AddIfMissing(
                missing,
                TypographyRole.TechnicalStrong,
                technicalStrong);

            if (includeSpecialRoles)
            {
                AddIfMissing(
                    missing,
                    TypographyRole.Handwritten,
                    handwritten);
                AddIfMissing(
                    missing,
                    TypographyRole.SpecialAlert,
                    specialAlert);
                AddIfMissing(
                    missing,
                    TypographyRole.SpecialComic,
                    specialComic);
            }

            return missing;
        }

        private static void AddIfMissing(
            ICollection<TypographyRole> missing,
            TypographyRole role,
            Object font)
        {
            if (font == null)
            {
                missing.Add(role);
            }
        }
    }
}
