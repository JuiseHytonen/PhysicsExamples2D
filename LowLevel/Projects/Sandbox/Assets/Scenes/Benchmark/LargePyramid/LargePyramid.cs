using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LowLevelPhysics;
using UnityEngine.LowLevelPhysics2D;
using UnityEngine.UIElements;
using TimeSpan = System.TimeSpan;

public class LargePyramid : MonoBehaviour,  PhysicsCallbacks.IContactCallback
{
    public static LargePyramid Instance;
    private readonly PhysicsMask m_GroundMask = new(1);
    private readonly PhysicsMask m_DestructibleMask = new(2);
    private readonly PhysicsMask m_ProjectileMask = new(3);
    private Turret m_leftTurret;
    private Turret m_rightTurret;


    private SandboxManager m_SandboxManager;
    private SceneManifest m_SceneManifest;
    private UIDocument m_UIDocument;
    private CameraManipulator m_CameraManipulator;
    private PhysicsShape.ContactFilter m_DestructibleContactFilter;
    private TextField m_joinCodeField;
    private Button m_leftButton;
    private Button m_rightButton;
    private Button m_shootButton;
    private Button m_hostButton;
    private Button m_clientButton;

    private bool m_leftButtonDown;
    private bool m_rightButtonDown;
    private int m_BaseCount;
    private Vector2 m_OldGravity;
    private float m_GravityScale;

    private void OnEnable()
    {
        if (Instance != null)
        {
            return;
            Destroy(Instance);
        }
        Instance = this;
        m_SandboxManager = FindFirstObjectByType<SandboxManager>();
        m_SceneManifest = FindFirstObjectByType<SceneManifest>();
        m_UIDocument = GetComponent<UIDocument>();
        //m_SandboxManager.SceneOptionsUI = m_UIDocument;

        m_CameraManipulator = FindFirstObjectByType<CameraManipulator>();
      //  m_CameraManipulator.CameraSize = 80f;
        //m_CameraManipulator.CameraPosition = new Vector2(0f, 79f);

        // Set up the scene reset action.
      //  m_SandboxManager.SceneResetAction = SetupScene;

        m_BaseCount = 90;
        m_OldGravity = PhysicsWorld.defaultWorld.gravity;
        m_GravityScale = 1f;
        var world = PhysicsWorld.defaultWorld;
        world.autoContactCallbacks = true;

        //SetupOptions();

        SetupScene();

        var root = m_UIDocument.rootVisualElement;

        m_joinCodeField = root.Q<TextField>("JoinCode");
        m_leftButton = root.Q<Button>("LeftButton");

        m_hostButton = root.Q<Button>("HostButton");
        m_clientButton = root.Q<Button>("ClientButton");
        m_shootButton = root.Q<Button>("ShootButton");
        m_leftButton = root.Q<Button>("LeftButton");
        m_rightButton = root.Q<Button>("RightButton");


        m_leftButton.SetVisibleInHierarchy(false);
        m_rightButton.SetVisibleInHierarchy(false);
        m_shootButton.SetVisibleInHierarchy(false);

        m_leftButton.RegisterCallback<MouseDownEvent>(evt => m_leftButtonDown = true, TrickleDown.TrickleDown);
        m_leftButton.RegisterCallback<MouseUpEvent>(evt => m_leftButtonDown = false, TrickleDown.TrickleDown);
        m_rightButton.RegisterCallback<MouseDownEvent>(evt => m_rightButtonDown = true, TrickleDown.TrickleDown);
        m_rightButton.RegisterCallback<MouseUpEvent>(evt => m_rightButtonDown = false, TrickleDown.TrickleDown);
        m_shootButton.RegisterCallback<MouseUpEvent>(evt => Shoot());
        m_hostButton.RegisterCallback<MouseUpEvent>(OnHostClicked, TrickleDown.TrickleDown);
        m_clientButton.RegisterCallback<MouseUpEvent>(OnClientClicked, TrickleDown.TrickleDown);
        Debug.developerConsoleEnabled = false;
    }

    private async void OnClientClicked(MouseUpEvent evt)
    {
        var success = await RelayHelper.Instance.StartClientWithRelay(GetJoinCode());
        if (success)
        {
            ShowMoveButtonsAndHideConnectButtons();
        }
    }

    private async void OnHostClicked(MouseUpEvent evt)
    {
        var code = await RelayHelper.Instance.StartHostWithRelay(4, "dtls");
        if (code != null)
        {
            ShowMoveButtonsAndHideConnectButtons();
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
            RpcTest.SendRotateMessageToOthers(rotation);
        }

        if (Keyboard.current.leftArrowKey.isPressed || m_leftButtonDown)
        {
            var rotation = MyTurret.RotateLeft();
            RpcTest.SendRotateMessageToOthers(rotation);
        }


        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Shoot();
        }
    }

    public void OnContactBegin2D(PhysicsEvents.ContactBeginEvent beginEvent)
    {
        return;
        if (beginEvent.shapeA.GetDensity() > 99)
        {
            beginEvent.shapeA.Destroy();
        }
        else
        {
            beginEvent.shapeB.Destroy();
        }

        Debug.Log(DateTime.UtcNow - m_startTime);
        return;
        const float radius = 10f;
        PhysicsWorld.defaultWorld.DrawCircle(beginEvent.shapeB.transform.position, radius, Color.orangeRed, 0.09f, PhysicsWorld.DrawFillOptions.All);
        var explodeDef = new PhysicsWorld.ExplosionDefinition { position = beginEvent.shapeB.transform.position, radius = radius, falloff = 2f, impulsePerLength = 90f };

        // Explode in all the worlds.
        using var worlds = PhysicsWorld.GetWorlds();
        foreach (var world in worlds)
            world.Explode(explodeDef);
        if (beginEvent.shapeA.body.bodyType != RigidbodyType2D.Static)
        {
            beginEvent.shapeA.Destroy();
        }
        if (beginEvent.shapeB.body.bodyType != RigidbodyType2D.Static)
        {
            beginEvent.shapeB.Destroy();
        }

    }


    public void OnContactEnd2D(PhysicsEvents.ContactEndEvent endEvent)
    {

    }

    private void FixedUpdate()
    {
        if (m_nextShootTime != DateTime.MinValue && m_nextShootTime <= DateTime.UtcNow)
        {
        //    DoShoot();
         //   m_nextShootTime = DateTime.MinValue;
        }
    }

    public void RotateOtherTurret(Vector2 rotation)
    {
        OtherTurret.SetRotation(rotation);
    }

    public void ShootAtTime(long ticks, bool isMe)
    {
        if (m_startTime == DateTime.MinValue)
        {
            m_startTime = DateTime.UtcNow;
        }

        var ticksTimeSpan = new TimeSpan(ticks);
        var timeSpan = m_startTime -DateTime.UtcNow + ticksTimeSpan;
        ShootAfter((int)timeSpan.TotalMilliseconds, isMe);
        //Invoke(nameof(DoShoot), (float)timeSpan.TotalMilliseconds / 1000f);
        //m_nextShootTime = m_startTime + new TimeSpan(ticks);
    }

    private async void ShootAfter(int msDelay, bool isMe)
    {
        await Task.Delay(msDelay);
        Debug.Log("delay " + msDelay);
       // ShootAfter(1000);
        DoShoot(isMe);
    }

    private DateTime m_startTime = DateTime.MinValue;
    private DateTime m_nextShootTime;
    private TimeSpan m_shootDelay = new TimeSpan(0, 0, 0, 0, 200);

    private Turret MyTurret => m_leftTurret;//RpcTest.Instance.IsHost ? m_leftTurret : m_rightTurret;
    private Turret OtherTurret => m_leftTurret;//!RpcTest.Instance.IsHost ? m_leftTurret : m_rightTurret;


    public void Shoot()
    {

        if (m_startTime == DateTime.MinValue)
        {
            RpcTest.SendShootMessageToOthers(m_shootDelay.Ticks);
            //m_startTime = DateTime.UtcNow;
            ShootAtTime(m_shootDelay.Ticks, true);
        }
        else
        {
            var ticks = (DateTime.UtcNow - m_startTime + m_shootDelay).Ticks;
            RpcTest.SendShootMessageToOthers(ticks);
            ShootAtTime(ticks, true);
        }
    }


    private void CreateTurrets()
    {
        m_leftTurret = new Turret(-70f, -40f);
        m_rightTurret = new Turret(70f, -40f);
    }

    private void DoShoot(bool isMe)
    {
              var capsuleRadius = 1;
            var capsuleLength = capsuleRadius;
            var capsuleGeometry = PolygonGeometry.CreateBox(new Vector2(1, 1));

            var bodyDef = new PhysicsBodyDefinition { bodyType = RigidbodyType2D.Dynamic, gravityScale = m_GravityScale, fastCollisionsAllowed = true };
            var shapeDef = new PhysicsShapeDefinition
            {
                //contactFilter = new PhysicsShape.ContactFilter { categories = m_ProjectileMask, contacts =  m_DestructibleMask | m_GroundMask },
                surfaceMaterial = new PhysicsShape.SurfaceMaterial { friction = 0.0f, bounciness = 0.3f },
                contactEvents = true,
                density = 100f
            };

            // Fire all the projectiles.
            var definitions = new NativeArray<PhysicsBodyDefinition>(1, Allocator.Temp);
            for (var i = 0; i < 1; ++i)
            {
                // Calculate the fire spread.
                var halfSpread = 1 * 0.5f;
                var fireDirection = isMe?MyTurret.GetRotation():OtherTurret.GetRotation();
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
                // Create the projectile shape.
              //  shapeDef.surfaceMaterial.customColor = m_SandboxManager.ShapeColorState;
                var body = bodies[i];
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

    private void SetupOptions()
    {
        var root = m_UIDocument.rootVisualElement;

        {
            // Menu Region (for camera manipulator).
            var menuRegion = root.Q<VisualElement>("menu-region");
            menuRegion.RegisterCallback<PointerEnterEvent>(_ => ++m_CameraManipulator.OverlapUI);
            menuRegion.RegisterCallback<PointerLeaveEvent>(_ => --m_CameraManipulator.OverlapUI);



            // Base Count.
            var baseCount = root.Q<SliderInt>("base-count");
            baseCount.value = m_BaseCount;
            baseCount.RegisterValueChangedCallback(evt =>
            {
                m_BaseCount = evt.newValue;
                SetupScene();
            });

            // Gravity Scale.
            var gravityScale = root.Q<Slider>("gravity-scale");
            gravityScale.value = m_GravityScale;
            gravityScale.RegisterValueChangedCallback(evt =>
            {
                m_GravityScale = evt.newValue;

                // Get the default world.
                var world = PhysicsWorld.defaultWorld;

                world.gravity = m_OldGravity * m_GravityScale;
            });

            // Fetch the scene description.
            var sceneDescription = root.Q<Label>("scene-description");
            sceneDescription.text = $"\"{m_SceneManifest.LoadedSceneName}\"\n{m_SceneManifest.LoadedSceneDescription}";
        }
    }

    private void SetupScene()
    {
        // Reset the scene state.
     //   m_SandboxManager.ResetSceneState();

        // Get the default world.
        var world = PhysicsWorld.defaultWorld;

        CreateTurrets();


        // Ground.
        {
            var groundBody = world.CreateBody(new PhysicsBodyDefinition { position = new Vector2(0f, -50f), bodyType = RigidbodyType2D.Static});

            var shapeDef = new PhysicsShapeDefinition
            {
              //  contactFilter = new PhysicsShape.ContactFilter { categories = m_GroundMask, contacts = m_ProjectileMask | m_DestructibleMask },
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

                    shapeDef.surfaceMaterial.customColor = m_SandboxManager.ShapeColorState;
                    body.CreateShape(boxGeometry, shapeDef);
                }
            }
        }
    }
}
