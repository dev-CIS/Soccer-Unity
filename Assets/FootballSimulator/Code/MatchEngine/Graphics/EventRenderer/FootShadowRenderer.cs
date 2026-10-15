
using UnityEngine;

namespace FStudio.MatchEngine.Graphics.EventRenderer {
    public class FootShadowRenderer : AbstractEventRenderer<FootShadowRenderer> {
        [SerializeField] private Transform[] m_prefabs;

        protected override Transform[] prefabs() => m_prefabs;

        public Transform Get () {
            var shadow = pool[0].Get();
            // the previous owner deactivates it on destroy.
            shadow.gameObject.SetActive(true);
            return shadow;
        }
    }
}