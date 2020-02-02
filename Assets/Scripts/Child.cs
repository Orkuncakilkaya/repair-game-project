using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class Child : MonoBehaviour
{
    public bool kaboom;
    public bool hasMagic;
    public float interpolationTime;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private Vector3 finalPosition;
    private Quaternion finalRotation;

    private Vector3 currentPosition;
    private Quaternion currentRotation;

    private Rigidbody rigidbody;
    private Parent parent;
    private bool hasMagicApplied;

    // Start is called before the first frame update
    void Start()
    {
        this.initialPosition = this.transform.position;
        this.initialRotation = this.transform.rotation;
        this.rigidbody = this.GetComponent<Rigidbody>();
        this.rigidbody.isKinematic = true;
        this.rigidbody.detectCollisions = false;
        this.parent = GameObject.FindObjectOfType<Parent>();
    }

    void Kaboom()
    {
        this.rigidbody.isKinematic = false;
        this.rigidbody.detectCollisions = true;
        this.rigidbody.mass = 3;
        this.kaboom = true;
        this.rigidbody.AddExplosionForce(750f, Vector3.zero, 100f, 3f);
    }

    // Update is called once per frame
    void Update()
    {
        this.updateAnimation();
        this.applyChanges();
        if (this.hasMagic != this.hasMagicApplied)
        {
            if (this.hasMagic)
            {
                this.rigidbody.isKinematic = true;
                this.rigidbody.detectCollisions = false;
                this.hasMagicApplied = true;
            }
            else
            {
                if (kaboom)
                {
                    this.rigidbody.isKinematic = false;
                    this.rigidbody.detectCollisions = true;
                    this.hasMagicApplied = false;
                }
            }
        }

        if (this.hasMagic)
        {
            this.interpolate();
        }
        else
        {
            this.finalPosition = this.transform.position;
            this.finalRotation = this.transform.rotation;
            currentPosition = this.finalPosition;
            currentRotation = this.currentRotation;
        }
    }

    private void interpolate()
    {
        currentPosition = Vector3.Slerp(this.finalPosition, this.initialPosition, this.interpolationTime);
        currentRotation = Quaternion.Slerp(this.finalRotation, this.initialRotation, this.interpolationTime);
    }

    private void updateAnimation()
    {
        if (!hasMagic)
        {
            return;
        }
        
        this.transform.position = Vector3.MoveTowards(this.transform.position, currentPosition, 1f);
        this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation, currentRotation, 10f);
    }

    private void applyChanges()
    {
        this.hasMagic = this.parent.hasMagic;
        this.interpolationTime = this.parent.interpolationTime;
        if (!this.kaboom && this.parent.kaboom)
        {
            this.Kaboom();
        }
    }
}