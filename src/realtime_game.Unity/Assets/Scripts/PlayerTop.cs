using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerTop : MonoBehaviour
{
    public float launchForceMultiplier = 10f;
    public float launchTorqueMultiplier = 200f;
    public float spinDecayRate = 1f; // ñàïbÇ«ÇÍÇæÇØäpë¨ìxÇå∏ÇÁÇ∑Ç©
    public float collisionSpinTransfer = 0.3f; // è’ìÀéûÇ…ëäéËÇ÷ìnÇ∑äpë¨ìxäÑçá
    public float health = 100f; // ÉQÅ[ÉÄìIHPÅiîCà”Åj

    Rigidbody rb;

    // input support
    private Vector3 dragStart;
    private bool dragging = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        // Yé≤âÒÇËÇÃÇ›âÒÇ∑ê›åvÇ»ÇÁëºÇÃé≤Çå≈íËÅiä»à’Åj
    }

    void Update()
    {
        // simple spin decay:
        rb.angularVelocity *= Mathf.Clamp01(1f - spinDecayRate * Time.deltaTime);

        // Input: mouse/touch drag to launch (single-player/local)
        if (Input.GetMouseButtonDown(0))
        {
            Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(r, out RaycastHit hit))
            {
                if (hit.collider != null && hit.collider.gameObject == gameObject)
                {
                    dragging = true;
                    dragStart = Input.mousePosition;
                }
            }
        }

        if (dragging && Input.GetMouseButtonUp(0))
        {
            Vector3 dragEnd = Input.mousePosition;
            Vector3 drag = dragEnd - dragStart;
            LaunchFromDrag(drag);
            dragging = false;
        }
    }

    void LaunchFromDrag(Vector3 drag)
    {
        // direction on XZ plane
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        Vector3 dir = (right * drag.x + Vector3.Cross(right, camForward) * drag.y).normalized; // crude mapping
        dir.y = 0;
        if (dir == Vector3.zero) dir = transform.forward;

        // power (clamp)
        float power = Mathf.Clamp(drag.magnitude / 100f, 0.2f, 2f);

        // add forward impulse and angular velocity
        rb.AddForce(dir * (power * launchForceMultiplier), ForceMode.Impulse);
        rb.AddTorque(Vector3.up * (power * launchTorqueMultiplier), ForceMode.Impulse);
    }

    void OnCollisionEnter(Collision other)
    {
        // if collide with another 'PlayerTop', transfer some spin and apply impulse
        PlayerTop otherTop = other.collider.GetComponent<PlayerTop>();
        if (otherTop != null)
        {
            // transfer a bit of spin
            Vector3 myAngular = rb.angularVelocity;
            Vector3 impart = myAngular * collisionSpinTransfer;
            otherTop.rb.AddTorque(impart, ForceMode.VelocityChange);
            // reduce own angular velocity proportional
            rb.angularVelocity = myAngular * (1f - collisionSpinTransfer);

            // simple damage model: reduce health based on relative velocity
            float impact = other.relativeVelocity.magnitude;
            health -= impact * 5f;
            otherTop.health -= impact * 5f;

            // add reactive force (knockback)
            Vector3 impulse = other.relativeVelocity.normalized * impact * 0.5f;
            rb.AddForce(-impulse, ForceMode.Impulse);
            otherTop.rb.AddForce(impulse, ForceMode.Impulse);

            // TODO: play sound/particles here
        }
    }

    public bool IsKnockedOut()
    {
        // knocked out when health low or spin below threshold
        return health <= 0f || rb.angularVelocity.magnitude < 0.5f;
    }
}
