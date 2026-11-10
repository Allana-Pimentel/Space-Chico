using UnityEngine;

namespace SpaceChico
{
    public class ParallaxLoop : MonoBehaviour
    {
        public Transform cam;
        public float parallaxFactor = 0.65f, verticalFollowFactor = 0.5f;
        public SpriteRenderer[] panels;

        private float panelWidth;
        private Vector3 lastCamPos;

        void Start()
        {
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            if (cam != null) lastCamPos = cam.position;
            if (panels == null || panels.Length == 0) panels = GetComponentsInChildren<SpriteRenderer>();

            if (panels?.Length >= 3 && panels[0].sprite != null)
            {
                panelWidth = panels[0].bounds.size.x - 0.02f;
                float cx = cam != null ? cam.position.x : transform.position.x;
                float cy = cam != null ? cam.position.y : transform.position.y;
                for (int i = 0; i < 3; i++)
                    panels[i].transform.position = new Vector3(cx + (i - 1) * panelWidth, cy, transform.position.z);
            }
        }

        void LateUpdate()
        {
            if (cam == null || panels == null || panels.Length < 3 || panelWidth <= 0f) return;
            Vector3 d = cam.position - lastCamPos;
            float totalW = panelWidth * panels.Length;

            foreach (var p in panels)
            {
                p.transform.position += new Vector3(d.x * parallaxFactor, d.y * verticalFollowFactor, 0f);
                float dist = cam.position.x - p.transform.position.x;
                if (dist > panelWidth * 1.5f) p.transform.position += new Vector3(totalW, 0f, 0f);
                else if (dist < -panelWidth * 1.5f) p.transform.position -= new Vector3(totalW, 0f, 0f);
            }
            lastCamPos = cam.position;
        }
    }
}
