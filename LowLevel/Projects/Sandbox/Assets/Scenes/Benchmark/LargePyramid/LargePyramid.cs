using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LowLevelPhysics2D;
using UnityEngine.UIElements;

public class LargePyramid : MonoBehaviour,  PhysicsCallbacks.IContactCallback
{
    private const int DEFAULT_SHOT_MANA_REDUCTION = 7;
    public static LargePyramid Instance;
    private Turret m_leftTurret;
    private Turret m_rightTurret;
    private UIDocument m_UIDocument;
    private PhysicsShape.ContactFilter m_DestructibleContactFilter;
    private TextField m_joinCodeField;
    private Button m_leftButton;
    private Button m_rightButton;
    private Button m_shootButton;
    private Button m_hostButton;
    private Button m_clientButton;
    private Button m_resetTimeButton;
    private Button m_leftManaElement;
    private Button m_rightManaElement;
    private Label m_notEnoughMana;

    private ManaCounter m_leftManaCounter;
    private ManaCounter m_rightManaCounter;

    private bool m_leftButtonDown;
    private bool m_rightButtonDown;
    private int m_BaseCount;
    private Vector2 m_OldGravity;
    private float m_GravityScale;
    private int m_fixedUpdates = 0;

    private bool m_nextShootIsMe;
    private int m_nextShootTime;
    private Vector2 m_nextShootAngle;
    private int shootDelayFixedUpdates = 50;

    private Turret MyTurret => RpcTest.Instance.IsHost ? m_leftTurret : m_rightTurret;
    private Turret OtherTurret => !RpcTest.Instance.IsHost ? m_leftTurret : m_rightTurret;

    private ManaCounter MyManaCounter => RpcTest.Instance.IsHost ? m_leftManaCounter : m_rightManaCounter;
    private ManaCounter OtherManaCounter => !RpcTest.Instance.IsHost ? m_leftManaCounter : m_rightManaCounter;

    private void OnEnable()
    {
        if (Instance != null)
        {
            Destroy(Instance);
        }
        Instance = this;
        m_UIDocument = GetComponent<UIDocument>();
        m_BaseCount = 90;
        m_OldGravity = PhysicsWorld.defaultWorld.gravity;
        m_GravityScale = 1f;
        var world = PhysicsWorld.defaultWorld;
        world.autoContactCallbacks = true;

        SetupScene();

        var root = m_UIDocument.rootVisualElement;

        m_joinCodeField = root.Q<TextField>("JoinCode");
        m_leftButton = root.Q<Button>("LeftButton");

        m_hostButton = root.Q<Button>("HostButton");
        m_clientButton = root.Q<Button>("ClientButton");
        m_shootButton = root.Q<Button>("ShootButton");
        m_leftButton = root.Q<Button>("LeftButton");
        m_rightButton = root.Q<Button>("RightButton");
        m_resetTimeButton = root.Q<Button>("ResetTimeButton");

        m_leftManaElement = root.Q<Button>("LeftMana");
        m_rightManaElement = root.Q<Button>("RightMana");
        m_notEnoughMana = root.Q<Label>("NotEnoughMana");
        m_notEnoughMana.SetVisibleInHierarchy(false);
        m_leftManaCounter = new ManaCounter(m_leftManaElement);
        m_rightManaCounter = new ManaCounter(m_rightManaElement);
        m_leftButton.SetVisibleInHierarchy(false);
        m_rightButton.SetVisibleInHierarchy(false);
        m_shootButton.SetVisibleInHierarchy(false);

        m_leftButton.RegisterCallback<MouseDownEvent>(evt => m_leftButtonDown = true, TrickleDown.TrickleDown);
        m_leftButton.RegisterCallback<MouseUpEvent>(evt => m_leftButtonDown = false, TrickleDown.TrickleDown);
        m_rightButton.RegisterCallback<MouseDownEvent>(evt => m_rightButtonDown = true, TrickleDown.TrickleDown);
        m_rightButton.RegisterCallback<MouseUpEvent>(evt => m_rightButtonDown = false, TrickleDown.TrickleDown);
        m_resetTimeButton.RegisterCallback<MouseUpEvent>(evt => m_fixedUpdates = 0, TrickleDown.TrickleDown);
        m_shootButton.RegisterCallback<MouseUpEvent>(evt => Shoot());
        m_hostButton.RegisterCallback<MouseUpEvent>(OnHostClicked, TrickleDown.TrickleDown);
        m_clientButton.RegisterCallback<MouseUpEvent>(OnClientClicked, TrickleDown.TrickleDown);
        Debug.developerConsoleEnabled = false;
    }

    public async void ResetTime(bool useDelay)
    {
        if (useDelay)
        {
            await Task.Delay(100);
        }
        m_fixedUpdates = 0;
    }


    private async void OnClientClicked(MouseUpEvent evt)
    {
        var success = await RelayHelper.Instance.StartClientWithRelay(GetJoinCode());
        if (success)
        {
            ShowMoveButtonsAndHideConnectButtons();
            await Task.Delay(200);
            RpcTest.Instance.SendResetRpc();
            ResetTime(true);
        }
    }

    private async void OnHostClicked(MouseUpEvent evt)
    {
        var code = await RelayHelper.Instance.StartHostWithRelay(4, "dtls");
        if (code != null)
        {
            ShowMoveButtonsAndHideConnectButtons();
            m_joinCodeField.value = code;
        }
    }

    private void ShowMoveButtonsAndHideConnectButtons()
    {
        m_hostButton.SetVisibleInHierarchy(false);
        m_clientButton.SetVisibleInHierarchy(false);
        m_leftButton.SetVisibleInHierarchy(true);
        m_rightButton.SetVisibleInHierarchy(true);
        m_shootButton.SetVisibleInHierarchy(true);
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            Shoot();
        }

        if (Keyboard.current.rightArrowKey.isPressed || m_rightButtonDown)
        {
            var rotation = MyTurret.RotateRight();
            RpcTest.Instance.SendRotateMessageToOthersRpc(rotation);
        }

        if (Keyboard.current.leftArrowKey.isPressed || m_leftButtonDown)
        {
            var rotation = MyTurret.RotateLeft();
            RpcTest.Instance.SendRotateMessageToOthersRpc(rotation);
        }


        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Shoot();
        }
    }

    public void OnContactBegin2D(PhysicsEvents.ContactBeginEvent beginEvent)
    {
        if (IsObjective(beginEvent.shapeA) || IsObjective(beginEvent.shapeB))
        {
            Debug.LogError("OBJECTIVE!!!");
            var projectile = IsObjective(beginEvent.shapeA) ? beginEvent.shapeB : beginEvent.shapeA;
            var winnerIsMe = projectile.userData.intValue == (int)PhysicsObjectType.OwnProjectile;
            var winnerManaCounter = winnerIsMe ? MyManaCounter : OtherManaCounter;
            var loserManaCounter = !winnerIsMe ? MyManaCounter : OtherManaCounter;
            winnerManaCounter.SetWinnerText("WINNER!");
            loserManaCounter.SetWinnerText("LOSER!");
        }

        bool IsObjective(PhysicsShape shape)
        {
            return shape.userData.intValue == (int)PhysicsObjectType.Objective;
        }
    }

    public void OnContactEnd2D(PhysicsEvents.ContactEndEvent endEvent)
    {

    }

    private void FixedUpdate()
    {
        m_fixedUpdates++;
        if (m_fixedUpdates == m_nextShootTime)
        {
            DoShoot(m_nextShootIsMe, m_nextShootAngle);
        }
    }

    public void RotateOtherTurret(Vector2 rotation)
    {
        OtherTurret.SetRotation(rotation);
    }

    public void ShootAtTime(int fixedUpdates, bool isMe, Vector2 rotation)
    {
        if(!isMe) Debug.Log("shoot message from other " + fixedUpdates +" vs " + m_fixedUpdates);
        m_nextShootIsMe = isMe;
        m_nextShootAngle = rotation;
        m_nextShootTime = fixedUpdates;
    }

    public void Shoot()
    {
        if (!MyManaCounter.HasManaAtLeast(DEFAULT_SHOT_MANA_REDUCTION))
        {
            m_notEnoughMana.ShowForMilliSeconds(1000);
            return;
        }
        m_notEnoughMana.SetVisibleInHierarchy(false);
        // Don´t allow shooting if another shot is pending. This will become obsolete as multiple queue is implemented
        if (m_nextShootTime > 0)
        {
            return;
        }
        RpcTest.Instance.SendShootMessageToOthersRpc(m_fixedUpdates + shootDelayFixedUpdates, MyTurret.GetRotation());
        ShootAtTime(m_fixedUpdates + shootDelayFixedUpdates, true, MyTurret.GetRotation());
    }

    private void CreateTurrets()
    {
        m_leftTurret = new Turret(-70f, -40f, Vector2.right);
        m_rightTurret = new Turret(70f, -40f, Vector2.left);
    }

    private void DoShoot(bool isMe, Vector2 rotation)
    {
        m_nextShootTime = 0;
        if (isMe)
        {
            MyManaCounter.ReduceMana(DEFAULT_SHOT_MANA_REDUCTION);
        }
        else
        {
            OtherManaCounter.ReduceMana(DEFAULT_SHOT_MANA_REDUCTION);
        }
        var capsuleRadius = 1;
        var capsuleLength = capsuleRadius;
        var capsuleGeometry = PolygonGeometry.CreateBox(new Vector2(1, 1));

        var bodyDef = new PhysicsBodyDefinition { bodyType = RigidbodyType2D.Dynamic, gravityScale = m_GravityScale, fastCollisionsAllowed = true };
        var shapeDef = new PhysicsShapeDefinition
        {
            //contactFilter = new PhysicsShape.ContactFilter { categories = m_ProjectileMask, contacts =  m_DestructibleMask | m_GroundMask },
            surfaceMaterial = new PhysicsShape.SurfaceMaterial { friction = 1f, bounciness = 0.3f },
            contactEvents = true,
            density = 100f,
            contactFilterCallbacks = true
        };

        // Fire all the projectiles.
        var definitions = new NativeArray<PhysicsBodyDefinition>(1, Allocator.Temp);
        for (var i = 0; i < 1; ++i)
        {
            // Calculate the fire spread.
            var halfSpread = 1 * 0.5f;
            var fireDirection = rotation;
            var fireSpeed = 90f;

            // Create the projectile body.
            bodyDef.position = isMe?MyTurret.GetPosition():OtherTurret.GetPosition();
            bodyDef.rotation = new PhysicsRotate(2f);
            bodyDef.linearVelocity = fireDirection * fireSpeed;

            definitions[i] = bodyDef;
        }

        // Create the bodies.
        using var bodies = PhysicsWorld.defaultWorld.CreateBodyBatch(definitions);

        // Create the capsules.
        for (var i = 0; i < 1; ++i)
        {
            var body = bodies[i];
            body.userData = new PhysicsUserData() { intValue = isMe ? (int)PhysicsObjectType.OwnProjectile : (int)PhysicsObjectType.OthersProjectile };
            body.callbackTarget = this;
            var shape =  body.CreateShape(capsuleGeometry, shapeDef);
            shape.callbackTarget = this;
        }

        // Dispose.
        definitions.Dispose();
    }

    private void OnDisable()
    {
        // Get the default world.
        var world = PhysicsWorld.defaultWorld;

        world.gravity = m_OldGravity;
    }

    public string GetJoinCode()
    {
        return m_joinCodeField.value;
    }

    private void SetupScene()
    {
        var world = PhysicsWorld.defaultWorld;

        CreateTurrets();

        // Ground.
        {
            var groundBody = world.CreateBody(new PhysicsBodyDefinition { position = new Vector2(0f, -50f), bodyType = RigidbodyType2D.Static});

            var shapeDef = new PhysicsShapeDefinition
            {
              //  contactFilter = new PhysicsShape.ContactFilter { categories = m_GroundMask, contacts = m_ProjectileMask | m_DestructibleMask },
              surfaceMaterial = new PhysicsShape.SurfaceMaterial { friction = 1f, bounciness = 0.3f }
            };
            const float groundLength = 1000f;
            groundBody.CreateShape(PolygonGeometry.CreateBox(new Vector2(groundLength, 2f)), shapeDef);
            groundBody.CreateShape(PolygonGeometry.CreateBox(new Vector2(groundLength, 2f), radius: 0f, new PhysicsTransform(new Vector2(0f, groundLength), PhysicsRotate.identity)), shapeDef);
            groundBody.CreateShape(PolygonGeometry.CreateBox(new Vector2(2f, groundLength), radius: 0f, new PhysicsTransform(new Vector2(groundLength * -0.5f, groundLength * 0.5f), PhysicsRotate.identity)), shapeDef);
            groundBody.CreateShape(PolygonGeometry.CreateBox(new Vector2(2f, groundLength), radius: 0f, new PhysicsTransform(new Vector2(groundLength * 0.5f, groundLength * 0.5f), PhysicsRotate.identity)), shapeDef);
        }
        return;
        // Pyramid.
        {
            var bodyDef = new PhysicsBodyDefinition { bodyType = RigidbodyType2D.Dynamic };
            var shapeDef = new PhysicsShapeDefinition
            {
               // contactFilter = new PhysicsShape.ContactFilter { categories = m_DestructibleMask, contacts = m_ProjectileMask | m_GroundMask },
                contactEvents = true
            };

            const float halfHeight = 0.5f;
            const float radius = 0.05f;
            var boxGeometry = PolygonGeometry.CreateBox(new Vector2(halfHeight - radius, halfHeight - radius) * 2f, radius);

            const float shift = 1.0f * halfHeight;

            for (var i = 0; i < m_BaseCount; ++i)
            {
                var y = (2.0f * i + 1.0f) * shift;

                for (var j = i; j < m_BaseCount; ++j)
                {
                    var x = (i + 1.0f) * shift + 2.0f * (j - i) * shift - halfHeight * m_BaseCount;

                    bodyDef.position = new Vector2(x, y);
                    var body = world.CreateBody(bodyDef);
                    body.CreateShape(boxGeometry, shapeDef);
                }
            }
        }
    }
}
