using UnityEngine;

namespace UI
{
    public static class RectTransformExtensions
    {
        /// <summary>
        /// Ensures the RectTransform stays fully within the parent (or canvas) bounds.
        /// </summary>
        /// <param name="rectTransform">The UI element to clamp.</param>
        /// <param name="parent">The parent container (e.g., canvas RectTransform).</param>
        /// <param name="padding">Optional padding to keep a margin inside the bounds.</param>
        public static void ClampToParent(this RectTransform rectTransform, RectTransform parent, Vector2 padding = default)
        {
            if (rectTransform == null || parent == null)
                return;

            var corners = new Vector3[4];
            var parentCorners = new Vector3[4];

            rectTransform.GetWorldCorners(corners);
            parent.GetWorldCorners(parentCorners);

            // Parent bounds in world space
            float parentMinX = parentCorners[0].x + padding.x;
            float parentMaxX = parentCorners[2].x - padding.x;
            float parentMinY = parentCorners[0].y + padding.y;
            float parentMaxY = parentCorners[2].y - padding.y;

            Vector3 position = rectTransform.position;

            // Clamp X
            if (corners[0].x < parentMinX)
                position.x += parentMinX - corners[0].x;
            else if (corners[2].x > parentMaxX)
                position.x -= corners[2].x - parentMaxX;

            // Clamp Y
            if (corners[0].y < parentMinY)
                position.y += parentMinY - corners[0].y;
            else if (corners[2].y > parentMaxY)
                position.y -= corners[2].y - parentMaxY;

            rectTransform.position = position;
        }
    }

}