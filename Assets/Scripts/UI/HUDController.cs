using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidRogues.Core;

namespace VoidRogues.UI
{
    /// <summary>
    /// Updates the in-game HUD in response to EventBus events.
    /// Subscribes to Sanity and Fragment changes — never polls GameManager directly.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Sanity")]
        [SerializeField] private Slider sanityBar;
        [SerializeField] private TextMeshProUGUI sanityText;

        [Header("Fragments")]
        [SerializeField] private TextMeshProUGUI fragmentsText;

        [Header("Corruption")]
        [SerializeField] private Slider corruptionBar;

        private void OnEnable()
        {
            EventBus.Subscribe<SanityChangedEvent>(OnSanityChanged);
            EventBus.Subscribe<FragmentsChangedEvent>(OnFragmentsChanged);
            EventBus.Subscribe<CorruptionChangedEvent>(OnCorruptionChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SanityChangedEvent>(OnSanityChanged);
            EventBus.Unsubscribe<FragmentsChangedEvent>(OnFragmentsChanged);
            EventBus.Unsubscribe<CorruptionChangedEvent>(OnCorruptionChanged);
        }

        private void Start()
        {
            // Initialise from current run data on scene load
            if (GameManager.Instance == null)
                return;

            var run = GameManager.Instance.Run;
            RefreshSanity(run.CurrentSanity, run.MaxSanity);
            RefreshFragments(run.Fragments);
            RefreshCorruption(run.Corruption);
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnSanityChanged(SanityChangedEvent evt)
            => RefreshSanity(evt.Current, evt.Max);

        private void OnFragmentsChanged(FragmentsChangedEvent evt)
            => RefreshFragments(evt.Current);

        private void OnCorruptionChanged(CorruptionChangedEvent evt)
            => RefreshCorruption(evt.Current);

        // ── Refresh helpers ───────────────────────────────────────────────────

        private void RefreshSanity(int current, int max)
        {
            if (sanityBar != null)
            {
                sanityBar.maxValue = max;
                sanityBar.value    = current;
            }
            if (sanityText != null)
                sanityText.text = $"{current} / {max}";
        }

        private void RefreshFragments(int current)
        {
            if (fragmentsText != null)
                fragmentsText.text = current.ToString();
        }

        private void RefreshCorruption(int current)
        {
            if (corruptionBar != null)
            {
                corruptionBar.maxValue = RunData.CORRUPTION_FULL_VOID;
                corruptionBar.value    = current;
            }
        }
    }
}
