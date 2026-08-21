using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework.Media;

namespace BazaarBounty
{
    public static class InputRouter
    {
        public enum InputType
        {
            MnK,
            GamePad,
        }

        public static InputType inputType = InputType.MnK;
        public static Vector2 lastMousePosition;

        public delegate void InputHandler();

        public delegate void InputAxisHandler(Vector2 axis);

        public delegate void InputTriggerHandler(float value);

        public delegate void InputMouseMovementHandler(Vector2 movement);

        private static Dictionary<Keys, InputHandler> inputKeyMappings = new();
        private static Dictionary<Keys, InputHandler> inputKeyDownMappings = new();
        private static Dictionary<Keys, bool> keyStates = new();
        private static Dictionary<Buttons, InputHandler> inputButtonMappings = new();
        private static Dictionary<Buttons, InputHandler> inputButtonDownMappings = new();
        private static Dictionary<Buttons, bool> buttonStates = new();
        public static InputAxisHandler leftThumbstickHandler;
        public static InputAxisHandler rightThumbstickHandler;
        public static InputTriggerHandler leftTriggerHandler;
        public static InputTriggerHandler rightTriggerHandler;
        public static InputMouseMovementHandler mouseMovementHandler;

        // Mouse button handlers
        public static InputHandler mouseLeftButtonHandler;
        public static InputHandler mouseRightButtonHandler;
        public static InputHandler mouseMiddleButtonHandler;

        static InputRouter()
        {
            // Global Input Registration
            RegisterInput(Keys.H, () => BazaarBountyGame.Settings.Debug.Mode = !BazaarBountyGame.Settings.Debug.Mode, true);
            RegisterInput(Keys.F10, () => BazaarBountyGame.GetGameInstance().ToggleFullScreen(), true);
            RegisterInput(Keys.O, () => MediaPlayer.Volume += 0.01f);
            RegisterInput(Keys.L, () => MediaPlayer.Volume -= 0.01f);
            RegisterInput(Keys.P, () =>
            {
                var game = BazaarBountyGame.GetGameInstance();
                if (game.CurrentGameState == BazaarBountyGame.GameState.Running)
                    game.PauseGame();
                else if (game.CurrentGameState == BazaarBountyGame.GameState.Pausing)
                    game.ContinueGame();
            }, true);
        }

        public static void RegisterInput(Keys key, InputHandler handler, bool keyDown = false)
        {
            if (keyDown)
            {
                keyStates.TryAdd(key, false);
                if (!inputKeyDownMappings.TryAdd(key, handler))
                    inputKeyDownMappings[key] += handler;
            }
            else
            {
                if (!inputKeyMappings.TryAdd(key, handler))
                    inputKeyMappings[key] += handler;
            }
        }

        public static void RegisterInput(Buttons button, InputHandler handler, bool keyDown = false)
        {
            if (keyDown)
            {
                buttonStates.TryAdd(button, false);
                if (!inputButtonDownMappings.TryAdd(button, handler))
                    inputButtonDownMappings[button] += handler;
            }
            else
            {
                if (!inputButtonMappings.TryAdd(button, handler))
                    inputButtonMappings[button] += handler;
            }
        }

        public static void DeregisterInput(Keys key, InputHandler handler, bool keyDown = false)
        {
            if (keyDown)
            {
                if (inputKeyDownMappings.ContainsKey(key))
                    inputKeyDownMappings[key] -= handler;
            }
            else
            {
                if (inputKeyMappings.ContainsKey(key))
                    inputKeyMappings[key] -= handler;
            }
        }

        public static void DeregisterInput(Buttons button, InputHandler handler, bool keyDown = false)
        {
            if (keyDown)
            {
                if (inputButtonDownMappings.ContainsKey(button))
                    inputButtonDownMappings[button] -= handler;
            }
            else
            {
                if (inputButtonMappings.ContainsKey(button))
                    inputButtonMappings[button] -= handler;
            }
        }

        public static void HandleInput()
        {
            // Handle Mouse input
            MouseState mouseState = Mouse.GetState();

            // Mouse movement
            Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);
            mouseMovementHandler?.Invoke(mousePosition);

            // Mouse button input
            if (mouseState.LeftButton == ButtonState.Pressed)
                mouseLeftButtonHandler?.Invoke();
            if (mouseState.RightButton == ButtonState.Pressed)
                mouseRightButtonHandler?.Invoke();
            if (mouseState.MiddleButton == ButtonState.Pressed)
                mouseMiddleButtonHandler?.Invoke();

            // check any key is pressed or mouse is moved
            if (Keyboard.GetState().GetPressedKeyCount() > 0 ||
                (mousePosition - lastMousePosition).LengthSquared() > 1e-4)
                inputType = InputType.MnK;
            else if (GamePad.GetState(PlayerIndex.One).IsConnected)
                inputType = InputType.GamePad;

            // Handle keyboard input
            foreach (Keys key in inputKeyMappings.Keys)
            {
                if (Keyboard.GetState().IsKeyDown(key))
                {
                    inputKeyMappings[key]?.Invoke();
                }
            }

            foreach (Keys key in inputKeyDownMappings.Keys)
            {
                if (!keyStates[key] && Keyboard.GetState().IsKeyDown(key))
                {
                    keyStates[key] = true;
                    inputKeyDownMappings[key]?.Invoke();
                }

                if (keyStates[key] && Keyboard.GetState().IsKeyUp(key))
                {
                    keyStates[key] = false;
                }
            }

            // Handle Gamepad input
            GamePadCapabilities gamePadCapabilities = GamePad.GetCapabilities(PlayerIndex.One);
            GamePadState state = GamePad.GetState(PlayerIndex.One);
            Vector2 input = Vector2.Zero;

            if (gamePadCapabilities.IsConnected)
            {
                // Check each button
                foreach (Buttons button in inputButtonMappings.Keys)
                {
                    if (state.IsButtonDown(button))
                    {
                        inputButtonMappings[button]?.Invoke();
                    }
                }

                foreach (Buttons button in inputButtonDownMappings.Keys)
                {
                    if (!buttonStates[button] && state.IsButtonDown(button))
                    {
                        buttonStates[button] = true;
                        inputButtonDownMappings[button]?.Invoke();
                    }

                    if (buttonStates[button] && state.IsButtonUp(button))
                    {
                        buttonStates[button] = false;
                    }
                }

                // Check the thumbsticks
                leftThumbstickHandler?.Invoke(state.ThumbSticks.Left);
                rightThumbstickHandler?.Invoke(state.ThumbSticks.Right);
                leftTriggerHandler?.Invoke(state.Triggers.Left);
                rightTriggerHandler?.Invoke(state.Triggers.Right);
            }
        }
    }

    public class CharacterController
    {
        public static TimeSpan inputMethodThreshold = TimeSpan.FromSeconds(0.5);
        public DateTime lastKeyboardInputTime;
        public DateTime lastGamePadInputTime;
        public List<Character> Characters => characters;
        protected List<Character> characters;

        public CharacterController()
        {
            characters = new List<Character>();
        }

        public virtual void Update(GameTime gameTime)
        {
        }

        public void AddCharacter(Character character)
        {
            characters.Add(character);
        }

        public void RemoveCharacter(Character character)
        {
            characters.Remove(character);
        }

        public void ClearCharacters()
        {
            characters.Clear();
        }

        public int GetControlledCharacterCount()
        {
            return characters.Count;
        }
    }

    public class PlayerController : CharacterController
    {
        public void MnKMoveHelper(Vector2 motion)
        {
            lastKeyboardInputTime = DateTime.Now;

            if ((lastKeyboardInputTime - lastGamePadInputTime).TotalSeconds > inputMethodThreshold.TotalSeconds)
            {
                characters[0].CharacterGameState.SwitchState(ECharacterGameState.Walk);
                characters[0].ControlType = CharacterControlType.MnK;
                characters[0].Move(motion);
            }
        }

        private InputRouter.InputHandler moveLeft;
        private InputRouter.InputHandler moveDown;
        private InputRouter.InputHandler moveRight;
        private InputRouter.InputHandler moveUp;
        private InputRouter.InputHandler dash;
        private InputRouter.InputHandler meleeAttack;
        private InputRouter.InputHandler rangedAttack;
        private InputRouter.InputAxisHandler moveAxis;
        private InputRouter.InputAxisHandler lookAxis;
        private InputRouter.InputMouseMovementHandler mouseMovement;

        public void RegisterControllerInput()
        {
            // In game input registration
            // mouse and keyboard
            InputRouter.RegisterInput(Keys.W, moveUp);
            InputRouter.RegisterInput(Keys.A, moveLeft);
            InputRouter.RegisterInput(Keys.S, moveDown);
            InputRouter.RegisterInput(Keys.D, moveRight);
            InputRouter.RegisterInput(Keys.LeftShift, dash);
            InputRouter.mouseMovementHandler = mouseMovement;
            InputRouter.mouseLeftButtonHandler = meleeAttack;
            InputRouter.mouseRightButtonHandler = rangedAttack;

            // gamepad
            InputRouter.RegisterInput(Buttons.RightTrigger, dash);
            InputRouter.RegisterInput(Buttons.LeftTrigger, dash);
            InputRouter.RegisterInput(Buttons.RightShoulder, meleeAttack);
            InputRouter.RegisterInput(Buttons.LeftShoulder, rangedAttack);
            InputRouter.leftThumbstickHandler = moveAxis;
            InputRouter.rightThumbstickHandler = lookAxis;
        }

        public void ClearControllerInput()
        {
            // mouse and keyboard
            InputRouter.DeregisterInput(Keys.W, moveUp);
            InputRouter.DeregisterInput(Keys.A, moveLeft);
            InputRouter.DeregisterInput(Keys.S, moveDown);
            InputRouter.DeregisterInput(Keys.D, moveRight);
            InputRouter.DeregisterInput(Keys.LeftShift, dash);
            InputRouter.mouseMovementHandler = null;
            InputRouter.mouseLeftButtonHandler = null;
            InputRouter.mouseRightButtonHandler = null;

            // gamepad
            InputRouter.DeregisterInput(Buttons.B, dash);
            InputRouter.DeregisterInput(Buttons.RightTrigger, meleeAttack);
            InputRouter.DeregisterInput(Buttons.LeftShoulder, rangedAttack);
            InputRouter.leftThumbstickHandler = null;
            InputRouter.rightThumbstickHandler = null;
        }

        public PlayerController()
        {

            // Input handler definitions
            moveLeft = () => MnKMoveHelper(new Vector2(-1, 0));
            moveDown = () => MnKMoveHelper(new Vector2(0, 1));
            moveRight = () => MnKMoveHelper(new Vector2(1, 0));
            moveUp = () => MnKMoveHelper(new Vector2(0, -1));

            meleeAttack = () => characters[0].MeleeAttack();
            rangedAttack = () => characters[0].RangedAttack();
            dash = () => characters[0].Dash();

            moveAxis = axis =>
            {
                axis.Y *= -1;
                if (axis.LengthSquared() > 0.5f)
                {
                    lastGamePadInputTime = DateTime.Now;
                    if (!((lastKeyboardInputTime - lastGamePadInputTime).TotalSeconds >
                          inputMethodThreshold.TotalSeconds))
                    {
                        characters[0].CharacterGameState.SwitchState(ECharacterGameState.Walk);
                        characters[0].ControlType = CharacterControlType.Controller;
                        characters[0].Move(axis);
                    }
                }
            };

            lookAxis = axis =>
            {
                axis.Y *= -1;
                if (axis.LengthSquared() > 0.1f)
                {
                    characters[0].Look(axis);
                    lastGamePadInputTime = DateTime.Now;
                }
            };

            mouseMovement = mousePosition =>
            {
                if (characters[0].ControlType == CharacterControlType.MnK)
                {
                    // Assume GetWorldPosition(mousePosition) converts screen to world coordinates if needed
                    Vector2 characterPosition = characters[0].Position; // Get your character's position here

                    // Calculate direction from character to mouse cursor
                    mousePosition = BazaarBountyGame.camera.ScreenToWorld(mousePosition);
                    Vector2 direction = mousePosition - characterPosition;

                    // Optionally, normalize the direction vector if you only need the direction and not the magnitude
                    direction.Normalize();

                    // Now, use the direction for character's looking direction
                    characters[0].Look(direction);
                }
            };
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            characters[0].Update(gameTime);
        }
    }

    public class EnemyController : CharacterController
    {
        public Dictionary<CharacterType, BehaviorTree> behaviorTrees = new();

        public EnemyController()
        {
            // action nodes
            BehaviorTree.BehaviorNode idle = new BehaviorTree.BehaviorNode(
                character =>
                {
                    var enemy = character as EnemyCharacter;
                    var nowTime = (float)BazaarBountyGame.GetGameTime().TotalGameTime.TotalSeconds;
                    if (nowTime - enemy.IdleStartTime >= enemy.IdleTotalTime)
                    {
                        Vector2 distanceFromSpawnPoint;
                        distanceFromSpawnPoint = character.Position - enemy.PatrolStartPoint;

                        if (distanceFromSpawnPoint.LengthSquared() <= 5f)
                        {
                            enemy.PatrolState = EnemyCharacter.PatrolStates.Departure;
                        }
                        else
                        {
                            enemy.PatrolState = EnemyCharacter.PatrolStates.Return;
                        }
                    }
                }
            );

            BehaviorTree.BehaviorNode navigateToPlayer = new BehaviorTree.BehaviorNode(
                character =>
                {
                    NavigationSystem.RegisterNavigationInfo(character, BazaarBountyGame.player);
                    Vector2 nextPosition = NavigationSystem.QueryCharacterNextPos(character);
                    Vector2 direction = nextPosition - character.Position;
                    if (direction.LengthSquared() > 0)
                    {
                        direction.Normalize();
                        character.LookDirection = direction;
                        character.Move(direction);
                    }
                }
            );

            BehaviorTree.BehaviorNode walkAwayFromPlayer = new BehaviorTree.BehaviorNode(
                character =>
                {
                    Vector2 dir2Player = BazaarBountyGame.player.Position - character.Position;
                    dir2Player.Normalize();
                    character.LookDirection = dir2Player;
                    character.Move(dir2Player * -1);
                }
            );

            BehaviorTree.BehaviorNode patrol = new BehaviorTree.BehaviorNode(
                character =>
                {
                    EnemyCharacter enemy = (EnemyCharacter)character;
                    Vector2 targetPosition = enemy.PatrolState == EnemyCharacter.PatrolStates.Departure
                        ? enemy.PatrolDestination
                        : enemy.PatrolStartPoint;
                    NavigationSystem.RegisterNavigationInfo(enemy, targetPosition);
                    Vector2 direction = targetPosition - character.Position;
                    Vector2 nextPosition = NavigationSystem.QueryCharacterNextPos(character);
                    Vector2 nextDirection = nextPosition - character.Position;
                    if (nextDirection.LengthSquared() > 0)
                    {
                        nextDirection.Normalize();
                        character.LookDirection = nextDirection;
                        character.Move(nextDirection);
                    }

                    if (direction.LengthSquared() <= 1f)
                    {
                        // Console.WriteLine($"{enemy} Arrived {targetPosition}");
                        character.CharacterGameState.ResetMoveState();
                        ((EnemyCharacter)character).PatrolState = EnemyCharacter.PatrolStates.Idle;
                        ((EnemyCharacter)character).IdleStartTime =
                            (float)BazaarBountyGame.GetGameTime().TotalGameTime.TotalSeconds;
                    }
                }
            );

            BehaviorTree.BehaviorNode meleeAtk = new BehaviorTree.BehaviorNode(character => character.MeleeAttack());

            BehaviorTree.BehaviorNode rangedAtk = new BehaviorTree.BehaviorNode(
                character =>
                {
                    Vector2 dir2Player = BazaarBountyGame.player.Position - character.Position;
                    dir2Player.Normalize();
                    character.LookDirection = dir2Player;
                    character.RangedAttack();
                }
            );

            BehaviorTree.BehaviorNode prepareAtk = new BehaviorTree.BehaviorNode(character =>
            {
                Vector2 dir2Player = BazaarBountyGame.player.Position - character.Position;
                dir2Player.Normalize();
                character.LookDirection = dir2Player;
                character.Move(-dir2Player); 
            });

            // conditions
            BehaviorTree.CheckCondition checkInAtkRange = character => ((EnemyCharacter)character).CheckInAttackRange();

            BehaviorTree.CheckCondition checkPatrolState = character =>
            {
                return ((EnemyCharacter)character).PatrolState == EnemyCharacter.PatrolStates.Idle;
            };

            BehaviorTree.CheckCondition checkVisibility = character =>
            {
                return ((EnemyCharacter)character).CheckPlayerInVision();
            };
            
            BehaviorTree.CheckCondition checkRangedVisibility = character =>
            {
                var enemy = (EnemyCharacter)character;
                if (!enemy.CheckPlayerInVision()) return false;
                Vector2 enemyPosition = character.Position;
                Vector2 playerPosition = BazaarBountyGame.player.Position;
                // Check if the bullet can hit the player (use bullet as flying object)
                return NavigationHelper.CheckLineIntersection(enemyPosition, playerPosition, CharacterType.Flying);
            };

            BehaviorTree.CheckCondition checkActionAvailable = character =>
            {
                return !character.State.HasFlag(ECharacterGameState.Dead) &&
                       !character.State.HasFlag(ECharacterGameState.Destroyed) &&
                       !character.State.HasFlag(ECharacterGameState.OnHit) &&
                       !character.State.HasFlag(ECharacterGameState.Stunned);
            };

            // Goblin AI
            BehaviorTree goblinAI = new BehaviorTree(new BehaviorTree.BehaviorNode(checkActionAvailable,
                new BehaviorTree.BehaviorNode(checkVisibility,
                    new BehaviorTree.BehaviorNode(checkInAtkRange,
                        meleeAtk,
                        navigateToPlayer),
                    new BehaviorTree.BehaviorNode(checkPatrolState,
                        idle,
                        patrol)),
                null
            ));

            // Ranged Goblin AI
            BehaviorTree rangedGoblinAI = new BehaviorTree(new BehaviorTree.BehaviorNode(checkActionAvailable,
                new BehaviorTree.BehaviorNode(checkRangedVisibility,
                    new BehaviorTree.BehaviorNode(checkInAtkRange,
                        walkAwayFromPlayer,
                        rangedAtk),
                    new BehaviorTree.BehaviorNode(checkPatrolState,
                        idle,
                        patrol)),
                null
            ));

            // Slime AI
            BehaviorTree.CheckCondition checkAtkGapFinish = character =>
            {
                Slime slime = (Slime)character;
                double currTime = BazaarBountyGame.GetGameTime().TotalGameTime.TotalSeconds;
                double lastAtkTime = slime.LastAttackTime;
                return currTime - lastAtkTime >= slime.AttackGapTime;
            };

            BehaviorTree.CheckCondition checkBatAtkGapFinish = character =>
            {
                Bat bat = (Bat)character;
                double currTime = BazaarBountyGame.GetGameTime().TotalGameTime.TotalSeconds;
                double lastAtkTime = bat.LastAttackTime;
                return currTime - lastAtkTime >= bat.AttackGapTime;
            };

            BehaviorTree slimeAI = new BehaviorTree(new BehaviorTree.BehaviorNode(checkActionAvailable,
                new BehaviorTree.BehaviorNode(checkVisibility,
                    new BehaviorTree.BehaviorNode(checkAtkGapFinish,
                        new BehaviorTree.BehaviorNode(checkInAtkRange,
                            meleeAtk,
                            navigateToPlayer
                        ),
                        prepareAtk
                    ),
                    new BehaviorTree.BehaviorNode(checkPatrolState,
                        idle,
                        patrol)),
                null
            ));

            BehaviorTree batAI = new BehaviorTree(new BehaviorTree.BehaviorNode(checkActionAvailable,
                new BehaviorTree.BehaviorNode(checkVisibility,
                    new BehaviorTree.BehaviorNode(checkBatAtkGapFinish,
                        new BehaviorTree.BehaviorNode(checkInAtkRange,
                            meleeAtk,
                            navigateToPlayer
                        ),
                        prepareAtk
                    ),
                    new BehaviorTree.BehaviorNode(checkPatrolState,
                        idle,
                        patrol)),
                null
            ));

            behaviorTrees.Add(CharacterType.Melee, goblinAI);
            behaviorTrees.Add(CharacterType.Slime, slimeAI);
            behaviorTrees.Add(CharacterType.Ranged, rangedGoblinAI);
            behaviorTrees.Add(CharacterType.Flying, batAI);
        }

        public override void Update(GameTime gameTime)
        {
            foreach (Character character in characters)
            {
                CharacterType type = character.Type;
                if (behaviorTrees.ContainsKey(type))
                {
                    behaviorTrees[type].Update(character);
                    character.Update(gameTime);
                }
            }

            // remove all dead enemies
            var collisionComponent = BazaarBountyGame.GetGameInstance().CollisionComp;
            foreach (var enemy in characters)
            {
                if (enemy.State.HasFlag(ECharacterGameState.Destroyed))
                {
                    collisionComponent.Remove(enemy);
                    NavigationSystem.DeregisterNavigationInfo(enemy);
                }
            }

            characters.RemoveAll(enemy => enemy.State.HasFlag(ECharacterGameState.Destroyed));
        }
    }

    public class BehaviorTree
    {
        public delegate void TakeAction(Character character);

        public delegate bool CheckCondition(Character character);

        public class BehaviorNode
        {
            public TakeAction action;
            public CheckCondition condition;
            public BehaviorNode yesNode;
            public BehaviorNode noNode;
            public bool isLeaf;

            // inner node
            public BehaviorNode(CheckCondition condition, BehaviorNode yesNode, BehaviorNode noNode)
            {
                this.condition = condition;
                this.yesNode = yesNode;
                this.noNode = noNode;
                isLeaf = false;
            }

            public BehaviorNode(CheckCondition condition)
            {
                this.condition = condition;
                isLeaf = false;
            }

            // leaf node
            public BehaviorNode(TakeAction action)
            {
                this.action = action;
                isLeaf = true;
            }
        }

        BehaviorNode root = null;

        public BehaviorTree(BehaviorNode root)
        {
            this.root = root;
        }

        public void Update(Character character)
        {
            var node = root;
            while (node != null)
            {
                if (node.isLeaf)
                {
                    node.action(character);
                    break;
                }
                else
                {
                    if (node.condition(character))
                    {
                        node = node.yesNode;
                    }
                    else
                    {
                        node = node.noNode;
                    }
                }
            }
        }
    }
}