﻿// Interact Use|Interactions|30060
namespace VRTK
{
    using UnityEngine;

    /// <summary>
    /// The Interact Use script is attached to a Controller object and requires the `VRTK_ControllerEvents` script to be attached as it uses this for listening to the controller button events for using and stop using interactable game objects.
    /// </summary>
    /// <remarks>
    /// It listens for the `AliasUseOn` and `AliasUseOff` events to determine when an object should be used and should stop using.
    ///
    /// The Controller object also requires the `VRTK_InteractTouch` script to be attached to it as this is used to determine when an interactable object is being touched. Only valid touched objects can be used.
    ///
    /// An object can be used if the Controller touches a game object which contains the `VRTK_InteractableObject` script and has the flag `isUsable` set to `true`.
    ///
    /// If a valid interactable object is usable then pressing the set `Use` button on the Controller (default is `Trigger`) will call the `StartUsing` method on the touched interactable object.
    /// </remarks>
    /// <example>
    /// `VRTK/Examples/006_Controller_UsingADoor` simulates using a door object to open and close it. It also has a cube on the floor that can be grabbed to show how interactable objects can be usable or grabbable.
    ///
    /// `VRTK/Examples/008_Controller_UsingAGrabbedObject` shows that objects can be grabbed with one button and used with another (e.g. firing a gun).
    /// </example>
    [RequireComponent(typeof(VRTK_ControllerEvents))]
    public class VRTK_InteractUse : MonoBehaviour
    {
        [Header("Use Settings")]

        [Tooltip("The button used to use/unuse a touched object.")]
        public VRTK_ControllerEvents.ButtonAlias useButton = VRTK_ControllerEvents.ButtonAlias.TriggerPress;

        [Header("Custom Settings")]

        [Tooltip("The Interact Touch script that is used to determine if an object is valid to use. If this is left blank then the Interact Touch script will be required to be on the same GameObject as this script.")]
        public VRTK_InteractTouch interactTouch;

        /// <summary>
        /// Emitted when a valid object starts being used.
        /// </summary>
        public event ObjectInteractEventHandler ControllerUseInteractableObject;
        /// <summary>
        /// Emitted when a valid object stops being used.
        /// </summary>
        public event ObjectInteractEventHandler ControllerUnuseInteractableObject;

        protected VRTK_ControllerEvents.ButtonAlias subscribedUseButton = VRTK_ControllerEvents.ButtonAlias.Undefined;
        protected VRTK_ControllerEvents.ButtonAlias savedUseButton = VRTK_ControllerEvents.ButtonAlias.Undefined;
        protected bool usePressed;
        protected VRTK_InteractTouch subscribedInteractTouch;

        protected GameObject usingObject = null;

        protected VRTK_ControllerActions controllerActions;
        protected VRTK_ControllerEvents controllerEvents;

        public virtual void OnControllerUseInteractableObject(ObjectInteractEventArgs e)
        {
            if (ControllerUseInteractableObject != null)
            {
                ControllerUseInteractableObject(this, e);
            }
        }

        public virtual void OnControllerUnuseInteractableObject(ObjectInteractEventArgs e)
        {
            if (ControllerUnuseInteractableObject != null)
            {
                ControllerUnuseInteractableObject(this, e);
            }
        }

        /// <summary>
        /// The GetUsingObject method returns the current object being used by the controller.
        /// </summary>
        /// <returns>The game object of what is currently being used by this controller.</returns>
        public virtual GameObject GetUsingObject()
        {
            return usingObject;
        }

        /// <summary>
        /// The ForceStopUsing method will force the controller to stop using the currently touched object and will also stop the object's using action.
        /// </summary>
        public virtual void ForceStopUsing()
        {
            if (usingObject != null)
            {
                StopUsing();
            }
        }

        /// <summary>
        /// The ForceResetUsing will force the controller to stop using the currently touched object but the object will continue with it's existing using action.
        /// </summary>
        public virtual void ForceResetUsing()
        {
            if (usingObject != null)
            {
                UnuseInteractedObject(false);
            }
        }

        protected virtual void Awake()
        {
            controllerActions = GetComponent<VRTK_ControllerActions>();
            controllerEvents = GetComponent<VRTK_ControllerEvents>();
        }

        protected virtual void OnEnable()
        {
            interactTouch = (interactTouch != null ? interactTouch : GetComponent<VRTK_InteractTouch>());
            if (!interactTouch)
            {
                Debug.LogError(VRTK_SharedMethods.GetCommonString("REQUIRED_SCRIPT_MISSING_FROM_GAMEOBJECT", new string[] { "VRTK_InteractTouch", "VRTK_InteractUse", "interactTouch" }));
                return;
            }

            ManageUseListener(true);
            ManageInteractTouchListener(true);
        }

        protected virtual void OnDisable()
        {
            ForceStopUsing();
            ManageUseListener(false);
            ManageInteractTouchListener(false);
        }

        protected virtual void Update()
        {
            ManageInteractTouchListener(true);
            ManageUseListener(true);
        }

        protected virtual void ManageInteractTouchListener(bool state)
        {
            if (subscribedInteractTouch && (!state || interactTouch != subscribedInteractTouch))
            {
                subscribedInteractTouch.ControllerTouchInteractableObject -= ControllerTouchInteractableObject;
                subscribedInteractTouch.ControllerUntouchInteractableObject -= ControllerUntouchInteractableObject;
                subscribedInteractTouch = null;
            }

            if (state && interactTouch && interactTouch != subscribedInteractTouch)
            {
                subscribedInteractTouch = interactTouch;
                subscribedInteractTouch.ControllerTouchInteractableObject += ControllerTouchInteractableObject;
                subscribedInteractTouch.ControllerUntouchInteractableObject += ControllerUntouchInteractableObject;
            }
        }

        protected virtual void ControllerTouchInteractableObject(object sender, ObjectInteractEventArgs e)
        {
            if (e.target)
            {
                VRTK_InteractableObject touchedObjectScript = e.target.GetComponent<VRTK_InteractableObject>();
                if (touchedObjectScript && touchedObjectScript.useOverrideButton != VRTK_ControllerEvents.ButtonAlias.Undefined)
                {
                    savedUseButton = subscribedUseButton;
                    useButton = touchedObjectScript.useOverrideButton;
                }
            }
        }

        protected virtual void ControllerUntouchInteractableObject(object sender, ObjectInteractEventArgs e)
        {
            if (e.target)
            {
                VRTK_InteractableObject touchedObjectScript = e.target.GetComponent<VRTK_InteractableObject>();
                if (!touchedObjectScript.IsUsing() && savedUseButton != VRTK_ControllerEvents.ButtonAlias.Undefined)
                {
                    useButton = savedUseButton;
                    savedUseButton = VRTK_ControllerEvents.ButtonAlias.Undefined;
                }
            }
        }

        protected virtual void ManageUseListener(bool state)
        {
            if (controllerEvents && subscribedUseButton != VRTK_ControllerEvents.ButtonAlias.Undefined && (!state || !useButton.Equals(subscribedUseButton)))
            {
                controllerEvents.UnsubscribeToButtonAliasEvent(subscribedUseButton, true, DoStartUseObject);
                controllerEvents.UnsubscribeToButtonAliasEvent(subscribedUseButton, false, DoStopUseObject);
                subscribedUseButton = VRTK_ControllerEvents.ButtonAlias.Undefined;
            }

            if (controllerEvents && state && useButton != VRTK_ControllerEvents.ButtonAlias.Undefined && !useButton.Equals(subscribedUseButton))
            {
                controllerEvents.SubscribeToButtonAliasEvent(useButton, true, DoStartUseObject);
                controllerEvents.SubscribeToButtonAliasEvent(useButton, false, DoStopUseObject);
                subscribedUseButton = useButton;
            }
        }

        protected virtual bool IsObjectUsable(GameObject obj)
        {
            return (interactTouch.IsObjectInteractable(obj) && obj.GetComponent<VRTK_InteractableObject>().isUsable);
        }

        protected virtual bool IsObjectHoldOnUse(GameObject obj)
        {
            if (obj)
            {
                var objScript = obj.GetComponent<VRTK_InteractableObject>();
                return (objScript && objScript.holdButtonToUse);
            }
            return false;
        }

        protected virtual int GetObjectUsingState(GameObject obj)
        {
            if (obj)
            {
                var objScript = obj.GetComponent<VRTK_InteractableObject>();
                if (objScript)
                {
                    return objScript.usingState;
                }
            }
            return 0;
        }

        protected virtual void SetObjectUsingState(GameObject obj, int value)
        {
            if (obj)
            {
                var objScript = obj.GetComponent<VRTK_InteractableObject>();
                if (objScript)
                {
                    objScript.usingState = value;
                }
            }
        }

        protected virtual void AttemptHaptics()
        {
            if (usingObject)
            {
                var doHaptics = usingObject.GetComponentInParent<VRTK_InteractHaptics>();
                if (doHaptics)
                {
                    doHaptics.HapticsOnUse(controllerActions);
                }
            }
        }

        protected virtual void ToggleControllerVisibility(bool visible)
        {
            if (usingObject)
            {
                var controllerAppearanceScript = usingObject.GetComponentInParent<VRTK_InteractControllerAppearance>();
                if (controllerAppearanceScript)
                {
                    controllerAppearanceScript.ToggleControllerOnUse(visible, controllerActions, usingObject);
                }
            }
        }

        protected virtual void UseInteractedObject(GameObject touchedObject)
        {
            if ((usingObject == null || usingObject != touchedObject) && IsObjectUsable(touchedObject))
            {
                usingObject = touchedObject;
                var usingObjectScript = usingObject.GetComponent<VRTK_InteractableObject>();

                if (!usingObjectScript.IsValidInteractableController(interactTouch.gameObject, usingObjectScript.allowedUseControllers))
                {
                    usingObject = null;
                    return;
                }

                usingObjectScript.StartUsing(interactTouch.gameObject);
                ToggleControllerVisibility(false);
                AttemptHaptics();
                OnControllerUseInteractableObject(interactTouch.SetControllerInteractEvent(usingObject));
            }
        }

        protected virtual void UnuseInteractedObject(bool completeStop)
        {
            if (usingObject != null)
            {
                var usingObjectCheck = usingObject.GetComponent<VRTK_InteractableObject>();
                if (usingObjectCheck && completeStop)
                {
                    usingObjectCheck.StopUsing(interactTouch.gameObject);
                }
                ToggleControllerVisibility(true);
                OnControllerUnuseInteractableObject(interactTouch.SetControllerInteractEvent(usingObject));
                usingObject = null;
            }
        }

        protected virtual GameObject GetFromGrab()
        {
            var grabScript = GetComponent<VRTK_InteractGrab>();
            if (grabScript)
            {
                return grabScript.GetGrabbedObject();
            }
            return null;
        }

        protected virtual void StopUsing()
        {
            SetObjectUsingState(usingObject, 0);
            UnuseInteractedObject(true);
        }

        protected virtual void DoStartUseObject(object sender, ControllerInteractionEventArgs e)
        {
            usePressed = true;
            GameObject touchedObject = interactTouch.GetTouchedObject();
            if (touchedObject == null)
            {
                touchedObject = GetFromGrab();
            }

            if (touchedObject != null && interactTouch.IsObjectInteractable(touchedObject))
            {
                var interactableObjectScript = touchedObject.GetComponent<VRTK_InteractableObject>();

                if (interactableObjectScript.useOnlyIfGrabbed && !interactableObjectScript.IsGrabbed())
                {
                    return;
                }

                UseInteractedObject(touchedObject);
                if (usingObject && !IsObjectHoldOnUse(usingObject))
                {
                    SetObjectUsingState(usingObject, GetObjectUsingState(usingObject) + 1);
                }
            }
        }

        protected virtual void DoStopUseObject(object sender, ControllerInteractionEventArgs e)
        {
            if (IsObjectHoldOnUse(usingObject) || GetObjectUsingState(usingObject) >= 2)
            {
                StopUsing();
            }
            usePressed = false;
        }
    }
}