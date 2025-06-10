using UnityEngine;

namespace UI
{
    public static class PositionRectTransform // Chat gippity
    {
        public static void PositionOnOverlayCanvas(Canvas canvas, Camera cam, RectTransform rectTransform, Vector3 worldPosition, Vector2 pivot)
        {
            // Set anchored position
            rectTransform.anchoredPosition = GetPositionOnOverlayCanvas(canvas, cam, rectTransform, worldPosition, pivot);
        }

        // Function to position RectTransform on Overlay Canvas to align with world position
        public static Vector2 GetPositionOnOverlayCanvas(Canvas canvas, Camera cam, RectTransform rectTransform, Vector3 worldPosition, Vector2 pivot)
        {
            // Convert world position to screen space
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);

            // Convert screen space to canvas space
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPoint, canvas.worldCamera, out Vector2 canvasPos);

            // Calculate offset based on pivot
            Vector2 pivotOffset = new Vector2(
                rectTransform.rect.width * (pivot.x - 0.5f),
                rectTransform.rect.height * (pivot.y - 0.5f)
            );

            // Return anchored position
            return canvasPos + pivotOffset;
        }
    }
}