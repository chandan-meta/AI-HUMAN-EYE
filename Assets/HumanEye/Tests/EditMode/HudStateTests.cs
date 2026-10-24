using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HumanEye.Tests
{
    public class HudStateTests
    {
        [Test]
        public void ExportedSpritesFollowPointerFocusAndLessonStates()
        {
            var control = new GameObject("Figma button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(HudState));
            var texture = new Texture2D(1, 1);
            var sprites = new Sprite[6];
            try
            {
                for (int i = 0; i < sprites.Length; i++) { sprites[i] = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * .5f); sprites[i].name = "State " + i; }
                var image = control.GetComponent<Image>();
                var button = control.GetComponent<Button>(); button.transition = Selectable.Transition.None;
                var state = control.GetComponent<HudState>(); state.target = image;
                state.normalSprite = sprites[0]; state.hoverSprite = sprites[1]; state.pressedSprite = sprites[2];
                state.focusSprite = sprites[3]; state.selectedSprite = sprites[4]; state.disabledSprite = sprites[5];
                var pointer = new PointerEventData(null) { button = PointerEventData.InputButton.Left };
                var focus = new BaseEventData(null);

                state.Set("selected", false); Assert.AreSame(sprites[0], image.sprite);
                state.Set("current", true); Assert.AreSame(sprites[4], image.sprite);
                state.OnSelect(focus); Assert.AreSame(sprites[3], image.sprite);
                state.OnPointerEnter(pointer); Assert.AreSame(sprites[1], image.sprite);
                state.OnPointerDown(pointer); Assert.AreSame(sprites[2], image.sprite);
                state.Set("unavailable", true); Assert.AreSame(sprites[5], image.sprite);
                state.Set("unavailable", false); Assert.AreSame(sprites[2], image.sprite);
                state.OnPointerUp(pointer); Assert.AreSame(sprites[1], image.sprite);
                state.OnPointerExit(pointer); Assert.AreSame(sprites[3], image.sprite);
                state.OnDeselect(focus); Assert.AreSame(sprites[4], image.sprite);
                state.Set("current", false); state.Set("done", true); Assert.AreSame(sprites[4], image.sprite);
                button.interactable = false; state.Set("done", false); Assert.AreSame(sprites[5], image.sprite);
                button.interactable = true;
                state.OnPointerEnter(pointer); state.OnPointerDown(pointer); state.OnSelect(focus);
                control.SetActive(false);
                typeof(HudState).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(state, null); // EditMode does not schedule this callback on an inactive object.
                control.SetActive(true);
                state.Set("current", false);
                Assert.AreSame(sprites[0], image.sprite, "Disabling clears transient pointer and focus states");
                Assert.AreEqual(Color.white, image.color, "Artwork must not inherit the old navy tint");
            }
            finally
            {
                Object.DestroyImmediate(control);
                foreach (var sprite in sprites) if (sprite) Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void ControlsWithoutExportedSpritesKeepTheirSavedColours()
        {
            var control = new GameObject("Legacy control", typeof(RectTransform), typeof(Image), typeof(HudState));
            try
            {
                var image = control.GetComponent<Image>(); var normal = new Color(.2f, .3f, .4f);
                image.color = normal;
                var state = control.GetComponent<HudState>(); state.target = image;
                state.Set("selected", true); Assert.AreEqual(state.selected, image.color);
                state.Set("selected", false); Assert.AreEqual(normal, image.color);
                state.Set("unavailable", true); Assert.AreEqual(state.unavailable, image.color);
                state.Set("unavailable", false); state.Set("complete", true); Assert.AreEqual(state.complete, image.color);
                state.Set("complete", false); Assert.AreEqual(normal, image.color);
                Assert.IsNull(image.sprite);
            }
            finally { Object.DestroyImmediate(control); }
        }
    }
}
