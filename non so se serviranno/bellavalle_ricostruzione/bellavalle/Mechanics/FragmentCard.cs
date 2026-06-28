using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using Bellavalle.Data;

namespace Bellavalle.Mechanics
{
    /// <summary>
    /// Carta fisica con un frammento di frase.
    /// Il player la afferra con XRGrabInteractable e la porta su uno SnapSlot.
    ///
    /// Gerarchia prefab:
    ///   FragmentCard (XRGrabInteractable + Rigidbody + BoxCollider)
    ///   ├── CardVisual (MeshRenderer — quad o card mesh)
    ///   └── FragmentText (TextMeshPro 3D, world space)
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class FragmentCard : MonoBehaviour
    {
        // ── Proprietà pubblica ─────────────────────────────────────────
        public int    FragmentIndex  { get; private set; }
        public bool   IsGrabbed      { get; private set; }
        public bool   IsInSlot       { get; private set; }
        public SnapSlot CurrentSlot  { get; private set; }

        // ── Riferimenti ────────────────────────────────────────────────
        [SerializeField] TMP_Text      fragmentText;
        [SerializeField] Renderer      cardRenderer;
        [SerializeField] Material      normalMaterial;
        [SerializeField] Material      grabbedMaterial;
        [SerializeField] Material      correctMaterial;
        [SerializeField] Material      wrongMaterial;

        // ── Cached ────────────────────────────────────────────────────
        XRGrabInteractable _grab;
        Rigidbody          _rb;
        Vector3            _homePosition;   // posizione originale (per reset)
        Quaternion         _homeRotation;

        SentenceReconstructionManager _manager;
        SentenceFragment              _fragment;

        // ── Init ───────────────────────────────────────────────────────
        public void Init(SentenceFragment fragment, int index,
                         SentenceReconstructionManager manager)
        {
            _fragment      = fragment;
            FragmentIndex  = index;
            _manager       = manager;
            _homePosition  = transform.position;
            _homeRotation  = transform.rotation;

            // Testo
            if (fragmentText != null)
                fragmentText.text = fragment.textIT;

            // Colore carta (opzionale per categorie grammaticali)
            if (cardRenderer != null && fragment.cardColor != Color.white)
            {
                var mat = new Material(normalMaterial);
                mat.color = fragment.cardColor;
                cardRenderer.material = mat;
            }
        }

        void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            _rb   = GetComponent<Rigidbody>();

            // Registra eventi grab/release
            _grab.selectEntered.AddListener(OnGrabbed);
            _grab.selectExited.AddListener(OnReleased);

            // Configura Rigidbody per VR
            _rb.useGravity      = false;
            _rb.interpolation   = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        void OnDestroy()
        {
            if (_grab == null) return;
            _grab.selectEntered.RemoveListener(OnGrabbed);
            _grab.selectExited.RemoveListener(OnReleased);
        }

        // ── Grab / Release ─────────────────────────────────────────────
        void OnGrabbed(SelectEnterEventArgs args)
        {
            IsGrabbed = true;

            // Se era in uno slot, notifica il manager
            if (IsInSlot && CurrentSlot != null)
            {
                bool wasCorrect = CurrentSlot.IsCorrect;
                CurrentSlot.OnCardRemoved();
                _manager.OnCardRemovedFromSlot(CurrentSlot.SlotIndex, wasCorrect);
                CurrentSlot = null;
                IsInSlot    = false;
            }

            SetMaterial(grabbedMaterial);

            // Disabilita temporaneamente la gravità mentre è in mano
            _rb.useGravity = false;
        }

        void OnReleased(SelectExitEventArgs args)
        {
            IsGrabbed = false;

            // Controlla se è vicino a uno slot (in caso il player l'ha posata vicino)
            // La logica principale è in SnapSlot.OnTriggerEnter
            SetMaterial(normalMaterial);
            _rb.useGravity = false;  // le carte fluttuano, non cadono

            // Se non è finita in nessuno slot, lerp di ritorno alla home
            if (!IsInSlot)
                StartCoroutine(ReturnHome());
        }

        // ── Slot interaction ───────────────────────────────────────────
        public void PlaceInSlot(SnapSlot slot)
        {
            IsInSlot    = true;
            CurrentSlot = slot;

            // Snap alla posizione dello slot
            transform.SetParent(slot.transform);
            StartCoroutine(SnapToSlot(slot.transform.position, slot.transform.rotation));

            // Disabilita fisiche mentre è nello slot
            _rb.isKinematic = true;
            _rb.useGravity  = false;
        }

        public void RemoveFromSlot()
        {
            IsInSlot    = false;
            CurrentSlot = null;
            transform.SetParent(null);
            _rb.isKinematic = false;
        }

        public void SetCorrectVisual(bool correct)
        {
            SetMaterial(correct ? correctMaterial : wrongMaterial);
        }

        // ── Forza accettazione (usato da ShowSolution) ─────────────────
        public void ForcePlace(SnapSlot slot)
        {
            PlaceInSlot(slot);
            SetMaterial(correctMaterial);
        }

        // ── Animazioni ─────────────────────────────────────────────────
        System.Collections.IEnumerator SnapToSlot(Vector3 targetPos, Quaternion targetRot)
        {
            float t = 0f;
            Vector3    startPos = transform.position;
            Quaternion startRot = transform.rotation;

            while (t < 1f)
            {
                t += Time.deltaTime / 0.12f;  // 120ms snap
                transform.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0, 1, t));
                transform.rotation = Quaternion.Slerp(startRot, targetRot, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            transform.position = targetPos;
            transform.rotation = targetRot;
        }

        System.Collections.IEnumerator ReturnHome()
        {
            yield return new WaitForSeconds(0.3f);  // aspetta un po' prima di tornare
            if (IsInSlot || IsGrabbed) yield break;  // è stata raccolta nel frattempo

            float t = 0f;
            Vector3    startPos = transform.position;
            Quaternion startRot = transform.rotation;

            while (t < 1f && !IsGrabbed && !IsInSlot)
            {
                t += Time.deltaTime / 0.4f;
                transform.position = Vector3.Lerp(startPos, _homePosition, Mathf.SmoothStep(0, 1, t));
                transform.rotation = Quaternion.Slerp(startRot, _homeRotation, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
        }

        // ── Visual helper ──────────────────────────────────────────────
        void SetMaterial(Material mat)
        {
            if (cardRenderer != null && mat != null)
                cardRenderer.material = mat;
        }
    }
}
