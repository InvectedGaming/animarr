using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class UpgradeSpecificationFixture : CoreTest<UpgradableSpecification>
    {
        public static object[] IsUpgradeTestCases =
        {
            new object[] { Quality.SDTV, 1, Quality.SDTV, 2, Quality.SDTV, UpgradeableRejectReason.None },
            new object[] { Quality.WEBDL720p, 1, Quality.WEBDL720p, 2, Quality.WEBDL720p, UpgradeableRejectReason.None },
            new object[] { Quality.SDTV, 1, Quality.SDTV, 1, Quality.SDTV, UpgradeableRejectReason.CustomFormatScore },
            new object[] { Quality.WEBDL720p, 1, Quality.HDTV720p, 2, Quality.Bluray720p, UpgradeableRejectReason.BetterQuality },
            new object[] { Quality.WEBDL720p, 1, Quality.HDTV720p, 2, Quality.WEBDL720p, UpgradeableRejectReason.BetterQuality },
            new object[] { Quality.WEBDL720p, 1, Quality.WEBDL720p, 1, Quality.WEBDL720p, UpgradeableRejectReason.CustomFormatScore },
            new object[] { Quality.WEBDL1080p, 1, Quality.WEBDL1080p, 1, Quality.WEBDL1080p, UpgradeableRejectReason.CustomFormatScore }
        };

        private void GivenAutoDownloadPropers(ProperDownloadTypes type)
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.DownloadPropersAndRepacks)
                  .Returns(type);
        }

        [Test]
        [TestCaseSource(nameof(IsUpgradeTestCases))]
        public void IsUpgradeTest(Quality current, int currentVersion, Quality newQuality, int newVersion, Quality cutoff, UpgradeableRejectReason expected)
        {
            GivenAutoDownloadPropers(ProperDownloadTypes.PreferAndUpgrade);

            var profile = new QualityProfile
            {
                UpgradeAllowed = true,
                Items = Qualities.QualityFixture.GetDefaultQualities()
            };

            Subject.IsUpgradable(
                       profile,
                       new QualityModel(current, new Revision(version: currentVersion)),
                       new List<CustomFormat>(),
                       new QualityModel(newQuality, new Revision(version: newVersion)),
                       new List<CustomFormat>())
                    .Should().Be(expected);
        }

        [Test]
        public void should_return_true_if_proper_and_download_propers_is_do_not_download()
        {
            GivenAutoDownloadPropers(ProperDownloadTypes.DoNotUpgrade);

            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(),
            };

            Subject.IsUpgradable(
                       profile,
                       new QualityModel(Quality.DVD, new Revision(version: 1)),
                       new List<CustomFormat>(),
                       new QualityModel(Quality.DVD, new Revision(version: 2)),
                       new List<CustomFormat>())
                    .Should().Be(UpgradeableRejectReason.None);
        }

        [Test]
        public void should_return_false_if_proper_and_autoDownloadPropers_is_do_not_prefer()
        {
            GivenAutoDownloadPropers(ProperDownloadTypes.DoNotPrefer);

            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(),
            };

            Subject.IsUpgradable(
                       profile,
                       new QualityModel(Quality.DVD, new Revision(version: 1)),
                       new List<CustomFormat>(),
                       new QualityModel(Quality.DVD, new Revision(version: 2)),
                       new List<CustomFormat>())
                   .Should().Be(UpgradeableRejectReason.UpgradesNotAllowed);
        }

        [Test]
        public void should_return_false_if_release_and_existing_file_are_the_same()
        {
            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities()
            };

            Subject.IsUpgradable(
                       profile,
                       new QualityModel(Quality.HDTV720p, new Revision(version: 1)),
                       new List<CustomFormat>(),
                       new QualityModel(Quality.HDTV720p, new Revision(version: 1)),
                       new List<CustomFormat>())
                   .Should().Be(UpgradeableRejectReason.UpgradesNotAllowed);
        }

        [Test]
        public void should_return_true_if_release_has_higher_quality_and_cutoff_is_not_already_met()
        {
            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                UpgradeAllowed = true,
                Cutoff = Quality.HDTV1080p.Id
            };

            Subject.IsUpgradable(
                    profile,
                    new QualityModel(Quality.HDTV720p, new Revision(version: 1)),
                    new List<CustomFormat>(),
                    new QualityModel(Quality.HDTV1080p, new Revision(version: 1)),
                    new List<CustomFormat>())
                .Should().Be(UpgradeableRejectReason.None);
        }

        [Test]
        public void should_return_false_if_release_has_higher_quality_and_cutoff_is_already_met()
        {
            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                UpgradeAllowed = true,
                Cutoff = Quality.HDTV720p.Id
            };

            Subject.IsUpgradable(
                    profile,
                    new QualityModel(Quality.HDTV720p, new Revision(version: 1)),
                    new List<CustomFormat>(),
                    new QualityModel(Quality.HDTV1080p, new Revision(version: 1)),
                    new List<CustomFormat>())
                .Should().Be(UpgradeableRejectReason.QualityCutoff);
        }

        [Test]
        public void should_return_false_if_minimum_custom_score_is_not_met()
        {
            var customFormatOne = new CustomFormat
            {
                Id = 1,
                Name = "One"
            };

            var customFormatTwo = new CustomFormat
            {
                Id = 2,
                Name = "Two"
            };

            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                UpgradeAllowed = true,
                MinUpgradeFormatScore = 11,
                CutoffFormatScore = 100,
                FormatItems = new List<ProfileFormatItem>
                {
                    new ProfileFormatItem
                    {
                        Format = customFormatOne,
                        Score = 10
                    },
                    new ProfileFormatItem
                    {
                        Format = customFormatTwo,
                        Score = 20
                    }
                }
            };

            Subject.IsUpgradable(
                    profile,
                    new QualityModel(Quality.DVD),
                    new List<CustomFormat> { customFormatOne },
                    new QualityModel(Quality.DVD),
                    new List<CustomFormat> { customFormatTwo })
                .Should().Be(UpgradeableRejectReason.MinCustomFormatScore);
        }

        [Test]
        public void should_return_true_if_minimum_custom_score_is_met()
        {
            var customFormatOne = new CustomFormat
            {
                Id = 1,
                Name = "One"
            };

            var customFormatTwo = new CustomFormat
            {
                Id = 2,
                Name = "Two"
            };

            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                UpgradeAllowed = true,
                MinUpgradeFormatScore = 10,
                CutoffFormatScore = 100,
                FormatItems = new List<ProfileFormatItem>
                {
                    new ProfileFormatItem
                    {
                        Format = customFormatOne,
                        Score = 10
                    },
                    new ProfileFormatItem
                    {
                        Format = customFormatTwo,
                        Score = 20
                    }
                }
            };

            Subject.IsUpgradable(
                    profile,
                    new QualityModel(Quality.DVD),
                    new List<CustomFormat> { customFormatOne },
                    new QualityModel(Quality.DVD),
                    new List<CustomFormat> { customFormatTwo })
                .Should().Be(UpgradeableRejectReason.None);
        }
        private static QualityProfile LanguageProfile(CustomFormat dual, CustomFormat dub, int cutoffFormatScore = 1000, bool upgradeAllowed = true)
        {
            return new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                UpgradeAllowed = upgradeAllowed,
                MinUpgradeFormatScore = 1,
                CutoffFormatScore = cutoffFormatScore,
                FormatItems = new List<ProfileFormatItem>
                {
                    new ProfileFormatItem { Format = dual, Score = 2500 },
                    new ProfileFormatItem { Format = dub, Score = 1400 }
                }
            };
        }

        [Test]
        public void language_upgrade_beats_a_lower_quality_new_release()
        {
            GivenAutoDownloadPropers(ProperDownloadTypes.PreferAndUpgrade);
            var dual = new CustomFormat("[Lang] Dual Audio") { Id = 1 };
            var dub = new CustomFormat("[Lang] English Dub") { Id = 2 };
            var profile = LanguageProfile(dual, dub);

            // Sub-only Bluray on disk, dual-audio WEB-DL available: language wins over quality.
            Subject.IsUpgradable(
                    profile,
                    new QualityModel(Quality.Bluray1080p),
                    new List<CustomFormat>(),
                    new QualityModel(Quality.WEBDL1080p),
                    new List<CustomFormat> { dual })
                .Should().Be(UpgradeableRejectReason.None);
        }

        [Test]
        public void language_downgrade_is_rejected_even_when_quality_is_better()
        {
            GivenAutoDownloadPropers(ProperDownloadTypes.PreferAndUpgrade);
            var dual = new CustomFormat("[Lang] Dual Audio") { Id = 1 };
            var dub = new CustomFormat("[Lang] English Dub") { Id = 2 };
            var profile = LanguageProfile(dual, dub);

            Subject.IsUpgradable(
                    profile,
                    new QualityModel(Quality.WEBDL1080p),
                    new List<CustomFormat> { dual },
                    new QualityModel(Quality.Bluray1080p),
                    new List<CustomFormat>())
                .Should().Be(UpgradeableRejectReason.CustomFormatScore);
        }

        [Test]
        public void language_upgrade_stops_once_custom_format_cutoff_is_met()
        {
            GivenAutoDownloadPropers(ProperDownloadTypes.PreferAndUpgrade);
            var dual = new CustomFormat("[Lang] Dual Audio") { Id = 1 };
            var dub = new CustomFormat("[Lang] English Dub") { Id = 2 };
            var profile = LanguageProfile(dual, dub, cutoffFormatScore: 1000);

            // Existing English dub (1400) already meets the 1000 cutoff, so no swap to dual audio.
            Subject.IsUpgradable(
                    profile,
                    new QualityModel(Quality.WEBDL1080p),
                    new List<CustomFormat> { dub },
                    new QualityModel(Quality.WEBDL1080p),
                    new List<CustomFormat> { dual })
                .Should().Be(UpgradeableRejectReason.CustomFormatCutoff);
        }

        [Test]
        public void language_upgrade_is_rejected_when_profile_does_not_allow_upgrades()
        {
            GivenAutoDownloadPropers(ProperDownloadTypes.PreferAndUpgrade);
            var dual = new CustomFormat("[Lang] Dual Audio") { Id = 1 };
            var dub = new CustomFormat("[Lang] English Dub") { Id = 2 };
            var profile = LanguageProfile(dual, dub, upgradeAllowed: false);

            Subject.IsUpgradable(
                    profile,
                    new QualityModel(Quality.WEBDL1080p),
                    new List<CustomFormat>(),
                    new QualityModel(Quality.WEBDL1080p),
                    new List<CustomFormat> { dual })
                .Should().Be(UpgradeableRejectReason.UpgradesNotAllowed);
        }

        [Test]
        public void formats_without_the_language_prefix_do_not_count_as_language()
        {
            GivenAutoDownloadPropers(ProperDownloadTypes.PreferAndUpgrade);
            var tier = new CustomFormat("Anime BD Tier 01") { Id = 3 };
            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                UpgradeAllowed = true,
                FormatItems = new List<ProfileFormatItem> { new ProfileFormatItem { Format = tier, Score = 1400 } }
            };

            profile.CalculateLanguageScore(new List<CustomFormat> { tier }).Should().Be(0);
        }
    }
}
