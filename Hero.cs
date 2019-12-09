using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hero : MonoBehaviour
{
    public ChekerGraund chekerGraund;
    Transform skin;
    CharacterController controller;
    public Animator animator;

    //run
    public float mainSpeed = 0.1f;
    public float powerSpeed;
    public float powerJamp;
    public float mainGravity = -9f;
    public float powerGravity;
    public float maximalSpeed = 22f;

    //swim
    public float mainSwimSpeed = 3f;

    public int coutLandedJump = 1;
    int realLandedJump;

    public bool iAnGround;
    public bool iAnWall;
    public bool iTachUp;
    public bool hookActive;
    public bool ChekingGraund = true;
    bool iPunchWall;
    bool lastAnimRotatIsLeft;
    bool iJamp;

    public LayerMask climb;
    Vector3 moveVector;

    void Start()
    {
        Application.targetFrameRate = 60;
        controller = GetComponent<CharacterController>();
        chekerGraund = GetComponentInChildren<ChekerGraund>();
        skin = transform.GetChild(0);
    }

    void Update()
    {
        LimitingSpeed();

        Animating();

        if (!hookActive)
            if (!chekerGraund.iStayAnWater)
            {
                var moveVector = GetVectorMove(gameObject.transform, powerSpeed, powerJamp, mainGravity, iAnWall);
                controller.Move(moveVector * Time.deltaTime);

                var promejutok = 160f / 18f;
                var countPromejutkov = 9 - powerJamp;
                var angle = -promejutok * countPromejutkov;
                if (angle > 80f)
                    angle = 80f;

                RotatorX(skin.gameObject, angle);

                powerSpeed = SpeedControl(powerSpeed, mainSpeed);
                powerJamp = GetPowerJamp(powerJamp);
            }
            else
                SwimControl();
    }

    private void FixedUpdate()
    {
        if (!hookActive)
            if (!chekerGraund.iStayAnWater && !hookActive)
            {
                if (ChekingGraund)
                {
                    iAnGround = chekerGraund.iStayAnGraund;
                    iAnWall = IStayAnWall(gameObject.transform);
                }
            }
    }

    void Animating()
    {
        animator.SetBool("wall", iAnWall);

        if (!chekerGraund.iStayAnWater)
        {
            animator.SetBool("swim", false);

            if (!iAnGround && !iJamp)
            {
                iJamp = true;
                animator.SetBool("foll", true);
                animator.SetTrigger("follTrig");
            }

            if (iAnGround)
            {
                animator.SetBool("foll", false);
                iJamp = false;

                if (Input.GetKeyDown(KeyCode.D) && lastAnimRotatIsLeft)
                {
                    StartCoroutine(TimeBoolAnimations("rotateR", 0.02f));
                    lastAnimRotatIsLeft = false;
                    animator.SetBool("walk", true);
                }

                if (Input.GetKeyDown(KeyCode.A) && !lastAnimRotatIsLeft)
                {
                    StartCoroutine(TimeBoolAnimations("rotateL", 0.02f));
                    lastAnimRotatIsLeft = true;
                    animator.SetBool("walk", true);
                }

                if (Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.A))
                    animator.SetBool("walk", false);

                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.A))
                    animator.SetBool("walk", true);

            }
            else
                animator.SetBool("walk", false);

        }
        else
        {
            if (Input.GetKey(KeyCode.W)
                || Input.GetKey(KeyCode.D)
                || Input.GetKey(KeyCode.S)
                || Input.GetKey(KeyCode.A))
            {
                animator.SetBool("swim", true);
            }
            else
                animator.SetBool("swim", false);
        }
    }

    void SwimControl()
    {
        float delta = 8f * Time.deltaTime;


        if (Input.GetKey(KeyCode.W))
        {
            RotatorX(skin.gameObject, 80f);
            powerJamp = SlerpFloat(powerJamp, mainSwimSpeed - mainGravity, delta);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            RotatorX(skin.gameObject, -80f);
            powerJamp = SlerpFloat(powerJamp, -mainSwimSpeed - mainGravity, delta);
        }
        else
            powerJamp = SlerpFloat(powerJamp, -mainGravity + 0.15f, delta * 2f);

        if (Input.GetKey(KeyCode.A))
        {
            RotatorX(skin.gameObject, 0f);
            powerSpeed = SlerpFloat(powerSpeed, -mainSpeed, delta * 0.8f);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            RotatorX(skin.gameObject, 0f);
            powerSpeed = SlerpFloat(powerSpeed, mainSpeed, delta * 0.8f);
        }
        else
            powerSpeed = SlerpFloat(powerSpeed, 0, delta * 0.8f);


        var moveVector = GetVectorMove(gameObject.transform, powerSpeed, powerJamp, mainGravity, iAnWall);
        controller.Move(moveVector * Time.deltaTime);
    }

    public void HookCat(Vector3 point, float lenghtHook, float intensivity, float powerRotation)
    {

        var myCoordinate = transform.position;
        var nowVector = new Vector3
            (point.x - myCoordinate.x,
            point.y - myCoordinate.y,
            point.z - myCoordinate.z);

        var CyclinVector = GetCyclinVector();

        if (nowVector.magnitude > lenghtHook)
        {
            powerJamp += (nowVector.y + CyclinVector.y) * intensivity * Time.deltaTime;
            powerSpeed += (nowVector.x + CyclinVector.x) * intensivity * Time.deltaTime;
        }

        var moveVector = GetVectorMove(gameObject.transform, powerSpeed, powerJamp, mainGravity, iAnWall);
        controller.Move((moveVector) * Time.deltaTime);

        float GetAngle(Vector3 vector)
        {
            return Mathf.Acos(Vector3.Dot(vector, Vector3.right) / (vector.magnitude * Vector3.right.magnitude));
        }

        Vector3 GetCyclinVector()
        {
            var globalAngle = GetAngle(nowVector);

            if (Input.GetKey(KeyCode.D))
                globalAngle -= Mathf.PI / 6f;

            if (Input.GetKey(KeyCode.A))
                globalAngle += Mathf.PI / 6f;

            var x = nowVector.magnitude * Mathf.Cos(globalAngle);
            var y = nowVector.magnitude * Mathf.Sin(globalAngle);

            if (point.y > myCoordinate.y)
                return new Vector3(x, y, nowVector.z);

            return new Vector3(x, -y, nowVector.z);
        }
    }

    public bool IStayAnGraund()
    {
        RaycastHit hit;
        RaycastHit hit2;

        if (Physics.Raycast(transform.position, transform.up, out hit, 0.3f) && !iTachUp)
        {
            iTachUp = true;
            powerJamp = -mainGravity;
        }
        else
            iTachUp = false;

        if (Physics.Raycast(transform.position, Vector3.down, out hit2, 0.3f))
            return true;

        return false;
    }

    public float GetPowerJamp(float power)
    {
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
            && (iAnGround || iAnWall)
            && realLandedJump <= coutLandedJump)
        {
            power = DoJamp(power, 17f);
        }

        if (iAnGround)
        {
            realLandedJump = 0;
            power = -mainGravity;
        }

        if (!iAnWall)
            power = FixedSlerpFloat(power, 0, 13f * Time.deltaTime, 0.2f);

        if (power < 0)
            power = 0;

        return power;
    }

    public float DoJamp(float power, float n)
    {
        StartCoroutine(LandendJamp());
        power = n;
        realLandedJump++;

        return power;
    }

    public Vector3 GetVectorMove(Transform obj, float speed, float gravity, float mainGravity, bool iAnWall)
    {
        var vector = obj.right * speed + obj.up * (gravity + mainGravity);

        if (iAnWall)
            powerJamp = SlerpFloat(powerJamp, -mainGravity, 0.5f);

        return vector;
    }

    public float SpeedControl(float speed, float mainSpeed)
    {
        if (Input.GetKey(KeyCode.D) && speed < mainSpeed)
            speed = FixedSlerpFloat(speed, mainSpeed, 14f * Time.deltaTime, 0.3f);
        else if (Input.GetKey(KeyCode.A) && speed > -mainSpeed)
            speed = FixedSlerpFloat(speed, -mainSpeed, 14f * Time.deltaTime, 0.3f);
        else
        {
            if (iAnGround || iAnWall)
                speed = FixedSlerpFloat(speed, 0, 14f * Time.deltaTime, 0.3f);
            else
                speed = FixedSlerpFloat(speed, 0, 4f * Time.deltaTime, 0.05f);
        }
        return speed;
    }

    public void RotatorX(GameObject obj, float z)
    {
        if (Input.GetKey(KeyCode.A))
            DoRotationPerson(obj, 0f, -180f, z);

        if (Input.GetKey(KeyCode.D))
            DoRotationPerson(obj, 0f, 0f, z);

        if (!Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A))
            DoRotationPerson(obj, 0f, obj.transform.eulerAngles.y, z);
    }

    public void DoRotationPerson(GameObject obj, float x, float y, float z)
    {
        var a = obj.transform.rotation;
        var b = Quaternion.Euler(x, y, z);
        var t = 6f * Time.deltaTime;

        obj.transform.rotation = Quaternion.Slerp(a, b, t);
    }

    public bool IStayAnWall(Transform obj)
    {
        RaycastHit hit;

        if ((Physics.Raycast(obj.position, obj.right, out hit, 0.3f)
            || Physics.Raycast(obj.position, -obj.right, out hit, 0.3f))
            && iPunchWall == false)
        {
            powerSpeed = 0f;
            iPunchWall = true;
        }
        else
            iPunchWall = false;

        if (Physics.Raycast(obj.position, obj.right, out hit, 0.3f, climb))
        {
            DoRotationPerson(skin.gameObject, 0f, 0f, 0f);
            return true;
        }
        if (Physics.Raycast(obj.position, -obj.right, out hit, 0.3f, climb))
        {
            DoRotationPerson(skin.gameObject, 0f, -180f, 0f);
            return true;
        }
        return false;
    }

    static float FixedSlerpFloat(float a, float b, float delta, float power)
    {
        if (Mathf.Abs(a - b) > power)
        {
            if (a > b)
                return a - delta;
            if (b > a)
                return a + delta;

            return a;
        }

        return b;
    }

    static float SlerpFloat(float a, float b, float delta)
    {
        if (Mathf.Abs(a - b) > delta)
        {
            if (a > b)
                return a - delta;
            if (b > a)
                return a + delta;

            return a;
        }

        return b;
    }

    private void LimitingSpeed()
    {
        if (powerSpeed > maximalSpeed)
            powerSpeed = maximalSpeed;
        else if (powerSpeed < -maximalSpeed)
            powerSpeed = -maximalSpeed;

        if (powerJamp + mainGravity > maximalSpeed)
            powerJamp = maximalSpeed - mainGravity;
        else if (powerJamp + mainGravity < -maximalSpeed)
            powerJamp = -maximalSpeed - mainGravity;
    }

    IEnumerator LandendJamp()
    {
        ChekingGraund = false;
        iAnGround = false;
        iAnWall = false;
        yield return new WaitForSeconds(0.1f);
        ChekingGraund = true;
    }

    IEnumerator TimeBoolAnimations(string name, float time)
    {
        animator.SetBool(name, true);
        yield return new WaitForSeconds(time);
        animator.SetBool(name, false);
    }
}