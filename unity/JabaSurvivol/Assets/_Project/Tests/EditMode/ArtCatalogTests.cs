using JabaSurvivol.Editor;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class ArtCatalogTests
    {
        [Test]
        public void Every_manifest_asset_loads_through_the_catalog()
        {
            var manifest = ArtImportSettings.ReadManifest();
            ArtCatalog.ClearForTests();

            foreach (var entry in manifest.assets)
            {
                var sprite = ArtCatalog.Get(entry.resourcePath);

                Assert.That(sprite, Is.Not.Null, entry.path);
                Assert.That(sprite.name, Is.Not.Empty, entry.path);
            }
        }

        [Test]
        public void Missing_asset_returns_the_requested_fallback()
        {
            ArtCatalog.ClearForTests();
            var fallback = SpriteFactory.Ring;

            var sprite = ArtCatalog.Get("Characters/does_not_exist", fallback);

            Assert.That(sprite, Is.SameAs(fallback));
        }

        [Test]
        public void Imported_sprite_uses_manifest_pivot_and_ppu()
        {
            var manifest = ArtImportSettings.ReadManifest();
            var entry = ArtImportSettings.FindEntry(manifest, ArtImportSettings.AssetRoot + "/Characters/player_torso.png");
            Assert.That(entry, Is.Not.Null);

            var importer = AssetImporter.GetAtPath(ArtImportSettings.AssetRoot + "/Characters/player_torso.png") as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(ArtImportSettings.PixelsPerUnit));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.alphaIsTransparency, Is.True);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Assert.That(settings.spritePivot.x, Is.EqualTo(entry.pivotX).Within(0.001f));
            Assert.That(settings.spritePivot.y, Is.EqualTo(entry.pivotY).Within(0.001f));
        }


        [Test]
        public void Environment_tiles_use_full_rect_meshes_for_tiling()
        {
            var importer = AssetImporter.GetAtPath(ArtImportSettings.AssetRoot + "/Environment/floor_tile.png") as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
        }

        [Test]
        public void Android_application_and_adaptive_icons_are_configured()
        {
            ProjectSetup.ConfigureIcons();

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtImportSettings.AssetRoot + "/App/icon.png");
            Assert.That(icon, Is.Not.Null);
            var applicationIcons = PlayerSettings.GetIcons(NamedBuildTarget.Android, IconKind.Application);
            Assert.That(applicationIcons, Is.Not.Null);
            Assert.That(applicationIcons.Length, Is.GreaterThan(0));
            Assert.That(applicationIcons[0], Is.Not.Null);

            var adaptiveFound = false;
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
            {
                if (!kind.ToString().Contains("Adaptive", System.StringComparison.Ordinal))
                {
                    continue;
                }

                adaptiveFound = true;
                foreach (var slot in PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind))
                {
                    Assert.That(slot.GetTexture(), Is.Not.Null);
                }
            }

            Assert.That(adaptiveFound, Is.True);
        }
        [Test]
        public void Category_atlases_are_present_and_packed()
        {
            var characters = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/_Project/Art/Atlas/Characters.spriteatlas");
            var weapons = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/_Project/Art/Atlas/WeaponsFx.spriteatlas");
            var ui = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/_Project/Art/Atlas/UI.spriteatlas");

            Assert.That(characters, Is.Not.Null);
            Assert.That(weapons, Is.Not.Null);
            Assert.That(ui, Is.Not.Null);
            Assert.That(SpriteAtlasExtensions.GetPackables(characters).Length, Is.GreaterThan(0));
            Assert.That(SpriteAtlasExtensions.GetPackables(weapons).Length, Is.GreaterThan(0));
            Assert.That(SpriteAtlasExtensions.GetPackables(ui).Length, Is.GreaterThan(0));
        }
    }
}
