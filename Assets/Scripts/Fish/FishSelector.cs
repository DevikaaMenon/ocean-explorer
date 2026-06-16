using UnityEngine;
using UnityEngine.InputSystem;

// Attach to the Player GameObject.
// Shoots a ray from the camera on left-click; if it hits a fish it shows
// the info panel. Clicking anywhere else hides it.
public class FishSelector : MonoBehaviour {
    [SerializeField] private FishInfoPanel InfoPanel;
    [SerializeField] private Camera MainCamera;
    [SerializeField] private float MaxClickDistance = 30f;

    void Update() {
        if (Mouse.current.leftButton.wasPressedThisFrame) {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = MainCamera.ScreenPointToRay(mousePos);
            RaycastHit[] hits = Physics.RaycastAll(ray, MaxClickDistance, Physics.AllLayers, QueryTriggerInteraction.Collide);
            foreach (var hit in hits) {
                var clickable = hit.collider.GetComponentInParent<FishClickable>();
                if (clickable != null && clickable.Data != null) {
                    InfoPanel.Show(clickable.Data);
                    return;
                }
            }
            InfoPanel.Hide();
        }
    }
}
