using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace VoidRogues
{
    [DefaultExecutionOrder(-20)]
    public class InputManager : MonoBehaviour
    {
        public static InputManager instance;

        public PlayerInput playerInput;

        public InputAction moveAction;
        [HideInInspector] public InputAction pointerAction;

        public InputAction ui_loadoutAction { get; private set; }
        public InputAction ui_inGameMenuAction { get; private set; }
        public InputAction ui_minimapAction { get; private set; }

        public InputAction ui_select { get; private set; }
        public InputAction ui_cancelAction { get; private set; }
        public InputAction ui_backAction { get; private set; }
        public InputAction ui_drop { get; private set; }
        public InputAction ui_inspect { get; private set; }

        public InputAction ui_cycleLeft { get; private set; }
        public InputAction ui_cycleRight { get; private set; }
        public InputAction ui_cycleSecondaryLeft { get; private set; }
        public InputAction ui_cycleSecondaryRight { get; private set; }

        public InputAction ui_console { get; private set; }
        public InputAction ui_submit { get; private set; }
        public InputAction ui_upArrow { get; private set; }
        public InputAction ui_downArrow { get; private set; }

        public Vector2 inputVector = Vector2.zero;
        public Vector2 pointerWorldPosition = Vector2.zero;

        public bool isUI_CancelDown = false;
        public bool isUI_LoadoutDown = false;

        public Dictionary<eInputAction, FInputState> PlayerInputs = new Dictionary<eInputAction, FInputState>();

        public delegate void OnFocusedWidgetChanged(GameObject newFocusedWidget);
        public static event OnFocusedWidgetChanged onFocusedWidgetChanged;

        public GameObject FocusedWidget { get; private set; }

        private string _lastControlScheme = "";
        public string ControlScheme { get { return _lastControlScheme; } }
        public System.Action<string> OnControlSchemeChanged;

        void Awake()
        {
            if (instance != null)
            {
                Destroy(this.gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(this);

            playerInput = GetComponent<PlayerInput>();
            DefineActions();
        }

        private void Start()
        {
            EnablePlayerActions();
        }

        private void DefineActions()
        {
            // Player Actions
            moveAction = playerInput.actions["Move"];
            pointerAction = playerInput.actions["Point"];

            PlayerInputs.Add(eInputAction.Attack, new FInputState(playerInput.actions["Attack"], false, false));
            PlayerInputs.Add(eInputAction.Special, new FInputState(playerInput.actions["Special"],  false, false));
            PlayerInputs.Add(eInputAction.Dodge, new FInputState(playerInput.actions["Dodge"], false, false));

            PlayerInputs.Add(eInputAction.SkillZero, new FInputState( playerInput.actions["SkillZero"], false, false));
            PlayerInputs.Add(eInputAction.SkillOne, new FInputState( playerInput.actions["SkillOne"],  false, false));

            PlayerInputs.Add(eInputAction.Interact, new FInputState(playerInput.actions["Interact"], false, false));

            PlayerInputs.Add(eInputAction.SwapWeapon, new FInputState(playerInput.actions["SwapWeapon"], false, false));
            PlayerInputs.Add(eInputAction.SwapItemZero, new FInputState(playerInput.actions["SwapItemZero"], false, false));
            PlayerInputs.Add(eInputAction.SwapItemOne, new FInputState(playerInput.actions["SwapItemOne"],  false, false));

            // UI Actions
            playerInput.actions["Navigate"].Enable();

            ui_select = playerInput.actions["Select"];
            ui_inspect = playerInput.actions["Inspect"];
            ui_drop = playerInput.actions["Drop"];
            ui_cancelAction = playerInput.actions["Cancel"];
            ui_backAction = playerInput.actions["BackActionGamepad"];

            ui_cycleLeft = playerInput.actions["CycleLeft"];
            ui_cycleRight = playerInput.actions["CycleRight"];
            ui_cycleSecondaryLeft = playerInput.actions["CycleSecondaryLeft"];
            ui_cycleSecondaryRight = playerInput.actions["CycleSecondaryRight"];

            ui_loadoutAction = playerInput.actions["Loadout"];
            ui_inGameMenuAction = playerInput.actions["InGameMenu"];
            ui_minimapAction = playerInput.actions["Minimap"];

            ui_console = playerInput.actions["Console"];
            ui_submit = playerInput.actions["Submit"];
            ui_upArrow = playerInput.actions["UpArrow"];
            ui_downArrow = playerInput.actions["DownArrow"];
        }

        private void EnablePlayerActions()
        {
            ui_select.Enable();
            ui_inspect.Enable();
            ui_cancelAction.Enable();
            ui_backAction.Enable();
            ui_drop.Enable();

            ui_cycleLeft.Enable();
            ui_cycleRight.Enable();
            ui_cycleSecondaryLeft.Enable();
            ui_cycleSecondaryRight.Enable();

            ui_loadoutAction.Enable();
            ui_inGameMenuAction.Enable();
            
            ui_console.Enable();
            ui_submit.Enable();
            ui_upArrow.Enable();
            ui_downArrow.Enable();

            moveAction.Enable();
        }

        private void UpdateFocusedWidget()
        {
            if (EventSystem.current == null)
                return;

            if (FocusedWidget != EventSystem.current.currentSelectedGameObject)
            {
                FocusedWidget = EventSystem.current.currentSelectedGameObject;

                if (FocusedWidget == null)
                {
                    Debug.Log("Focused Widget Changed: Null");
                }
                else
                {
                    Debug.Log("Focused Widget Changed: " + FocusedWidget.name, FocusedWidget);
                }

                if (onFocusedWidgetChanged != null)
                    onFocusedWidgetChanged(FocusedWidget);
            }
        }

        public void ClearFocusedWidget()
        {
            if (FocusedWidget != null)
                FocusedWidget = null; // TODO: UNITY BASE CLASS?

            FocusedWidget = null;
        }

        private void TrackControlScheme()
        {
            if(playerInput.currentControlScheme != _lastControlScheme)
            {
                _lastControlScheme = playerInput.currentControlScheme;
                Debug.Log("ControlScheme changed to: " + playerInput.currentControlScheme);
                if (OnControlSchemeChanged != null)
                    OnControlSchemeChanged.Invoke(_lastControlScheme);
            }
            
        }

        private void Update()
        {
            inputVector = Vector2.zero;
            pointerWorldPosition = Vector2.zero;

            TrackControlScheme();

            if (!Application.isFocused)
            {
                inputVector = default;
                pointerWorldPosition = default;

                SetDown(eInputAction.Attack,false);
                SetDown(eInputAction.Special, false);
                SetDown(eInputAction.Dodge, false);
                SetDown(eInputAction.Interact, false);

                SetDown(eInputAction.SkillZero, false);
                SetDown(eInputAction.SkillOne, false);

                SetDown(eInputAction.SwapWeapon, false);
                SetDown(eInputAction.SwapItemZero, false);
                SetDown(eInputAction.SwapItemOne, false);

                isUI_CancelDown = false;
                isUI_LoadoutDown = false;
                return;
            }

            UpdateFocusedWidget();

            inputVector = moveAction.ReadValue<Vector2>();

            if (IsUsingMouseCursor())
            {
                Vector2 screenPosition = pointerAction.ReadValue<Vector2>();
                if (Camera.main != null)
                {
                    pointerWorldPosition = Camera.main.ScreenToWorldPoint(
                        new Vector3(screenPosition.x, screenPosition.y, Camera.main.nearClipPlane));
                }
            }

            UpdateInput(eInputAction.Attack);
            UpdateInput(eInputAction.Special);
            UpdateInput(eInputAction.Dodge);
            UpdateInput(eInputAction.Interact);

            UpdateInput(eInputAction.SkillZero);
            UpdateInput(eInputAction.SkillOne);

            UpdateInput(eInputAction.SwapWeapon);
            UpdateInput(eInputAction.SwapItemZero);
            UpdateInput(eInputAction.SwapItemOne);

            isUI_CancelDown = ui_cancelAction.ReadValue<float>() >= 0.5f;
            isUI_LoadoutDown = ui_loadoutAction.ReadValue<float>() >= 1.0f;

        }

        // Handles an input that is presslocked to ensure it is always not pressed
        // until a full depress
        private void UpdateInput(eInputAction inputAction)
        {
            FInputState state = PlayerInputs[inputAction];

            float readValue = state.action.ReadValue<float>();
            if (state.isLocked)
            {
                if (readValue < 0.5f)
                    state.isLocked = false;

                state.isDown = false;
            }
            else
            {
                state.isDown = readValue >= 0.5f;
            }

            PlayerInputs[inputAction] = state;
        }

        private void SetDown(eInputAction inputAction, bool isDown)
        {
            if (PlayerInputs.TryGetValue(inputAction, out var state))
            {
                state.isDown = isDown;
                PlayerInputs[inputAction] = state;
            }
        }

        // Used to lock out inputs until they have a full release
        // for when a UI view closes and prevents players from performing actions
        // accidentally on hold.
        public void LockPlayerInputs()
        {
            // Create a list of keys to iterate over, preventing modification issues
            List<eInputAction> keys = new List<eInputAction>(PlayerInputs.Keys);

            foreach (var key in keys)
            {
                // Retrieve the struct from the dictionary
                FInputState inputState = PlayerInputs[key];

                // Modify the struct
                inputState.isLocked = true;

                // Assign the modified struct back to the dictionary
                PlayerInputs[key] = inputState;
            }
        }

        private bool IsUsingMouseCursor()
        {
            return _lastControlScheme == "Keyboard&Mouse";
        }

        public struct FInputState
        {
            public InputAction action;
            public bool isDown;
            public bool isLocked;

            // Constructor to initialize all member variables
            public FInputState(InputAction action, bool isDown, bool isLocked)
            {
                this.action = action;
                this.isDown = isDown;
                this.isLocked = isLocked;
                this.action.Enable();
            }
        }
    }

    public enum eInputAction
    {
        None,
        Attack,
        Special,
        Dodge,
        Interact,

        SkillZero,
        SkillOne,

        SwapWeapon,
        SwapItemZero,
        SwapItemOne,

        MonsterSkill1,
        MonsterSkill2,
        MonsterSkill3,
        MonsterSkill4,
        MonsterSkill5,
        MonsterSkill6,
        MonsterSkill7,
        MonsterSkill8,

        UI_Cancel,
        UI_Loadout,
    }

    public enum eActionType : byte
    {
        None,

        Attack,
        Special,
        Dodge,
        Interact,
        SkillZero,
        SkillOne,

        DodgeAttack,
        DodgeSpecial,
        DodgeSkillZero,
        DodgeSkillOne,

        MonsterAttack1,
        MonsterAttack2,
        MonsterAttack3,
        MonsterAttack4,
        MonsterAttack5,
        MonsterAttack6,
        MonsterAttack7,
        MonsterAttack8,
    }

    public enum eButtonState
    {
        None,
        Pressed,
        Held,
        Released,
    }
}