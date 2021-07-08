﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Input.Touch
{
    public class TouchPanelState
    {
        /// <summary>
        /// The reserved touchId for all mouse touch points.
        /// </summary>
        private const int MouseTouchId = 1;

        /// <summary>
        /// The current touch state.
        /// </summary>
        private readonly List<TouchLocationData> _touchState = new List<TouchLocationData>();

        /// <summary>
        /// The current gesture state.
        /// </summary>
        private readonly List<TouchLocationData> _gestureState = new List<TouchLocationData>();

        /// <summary>
        /// The positional scale to apply to touch input.
        /// </summary>
        private Vector2 _touchScale = Vector2.One;

        /// <summary>
        /// The current size of the display.
        /// </summary>
        private Point _displaySize = Point.Zero;

        /// <summary>
        /// The next touch location identifier.
        /// The value 1 is reserved for the mouse touch point.
        /// </summary>
        private int _nextTouchId = 2;

        /// <summary>
        /// The current timestamp that we use for setting the timestamp of new TouchLocations
        /// </summary>
        internal static TimeSpan CurrentTimestamp { get; set; }

        /// <summary>
        /// The mapping between platform specific touch ids
        /// and the touch ids we assign to touch locations.
        /// </summary>
        private readonly Dictionary<int, int> _touchIds = new Dictionary<int, int>();

        internal readonly Queue<GestureSample> GestureList = new Queue<GestureSample>();

        private TouchPanelCapabilities Capabilities = new TouchPanelCapabilities();

        internal readonly GameWindow Window;

        internal TouchPanelState(GameWindow window)
        {
            Window = window;
        }

        /// <summary>
        /// The window handle of the touch panel. Purely for Xna compatibility.
        /// </summary>
        public IntPtr WindowHandle { get; set; }

        /// <summary>
        /// Returns capabilities of touch panel device.
        /// </summary>
        /// <returns><see cref="TouchPanelCapabilities"/></returns>
        public TouchPanelCapabilities GetCapabilities()
        {
            Capabilities.Initialize();
            return Capabilities;
        }

        /// <summary>
        /// Age all the touches, so any that were Pressed become Moved, and any that were Released are removed
        /// </summary>
        private void AgeTouches(List<TouchLocationData> state)
        {
            for (var i = state.Count - 1; i >= 0; i--)
            {
                var touch = state[i];
                switch (touch.State)
                {
                    case TouchLocationState.Released:
                        state.RemoveAt(i);
                        break;
                    case TouchLocationState.Pressed:
                    case TouchLocationState.Moved:
                        touch.AgeState();
                        state[i] = touch;
                        break;
                }
            }
        }

        /// <summary>
        /// Apply the given new touch to the state. If it is a Pressed it will be added as a new touch, otherwise we update the existing touch it matches
        /// </summary>
        private void ApplyTouch(List<TouchLocationData> state, TouchLocationData touch)
        {
            if (touch.State == TouchLocationState.Pressed)
            {
                state.Add(touch);
                return;
            }

            //Find the matching touch
            for (var i = 0; i < state.Count; i++)
            {
                var existingTouch = state[i];

                if (existingTouch.Id == touch.Id)
                {
                    //If we are moving straight from Pressed to Released and we've existed for multiple frames, that means we've never been seen, so just get rid of us
                    if (existingTouch.State == TouchLocationState.Pressed && touch.State == TouchLocationState.Released && existingTouch.PressTimestamp != touch.Timestamp)
                    {
                        state.RemoveAt(i);
                    }
                    else
                    {
                        //Otherwise update the touch based on the new one
                        existingTouch.UpdateState(touch);
                        state[i] = existingTouch;
                    }

                    break;
                }
            }
        }

        public TouchCollection GetState()
        {
            //Clear out touches from previous frames that were released on the same frame they were touched that haven't been seen
            for (var i = _touchState.Count - 1; i >= 0; i--)
            {
                var touch = _touchState[i];

                //If a touch was pressed and released in a previous frame and the user didn't ask about it then trash it.
                if (touch.SameFrameReleased && touch.Timestamp < CurrentTimestamp && touch.State == TouchLocationState.Pressed)
                {
                    _touchState.RemoveAt(i);
                }
            }

            var result = (_touchState.Count > 0) ? new TouchCollection(_touchState) : TouchCollection.Empty;
            AgeTouches(_touchState);
            return result;
        }

        internal void AddEvent(int id, TouchLocationState state, Vector2 position)
        {
            AddEvent(id, state, position, false);
        }

        internal void AddEvent(int id, TouchLocationState state, Vector2 position, bool isMouse)
        {
            // Different platforms return different touch identifiers
            // based on the specifics of their implementation and the
            // system drivers.
            //
            // Sometimes these ids are suitable for our use, but other
            // times it can recycle ids or do cute things like return
            // the same id for double tap events.
            //
            // We instead provide consistent ids by generating them
            // ourselves on the press and looking them up on move 
            // and release events.
            // 
            if (state == TouchLocationState.Pressed)
            {
                if (isMouse)
                {
                    // Mouse pointing devices always use a reserved Id
                    _touchIds[id] = MouseTouchId;
                }
                else
                {
                    _touchIds[id] = _nextTouchId++;
                }
            }

            // Try to find the touch id.
            int touchId;
            if (!_touchIds.TryGetValue(id, out touchId))
            {
                // If we got here that means either the device is sending
                // us bad, out of order, or old touch events.  In any case
                // just ignore them.
                return;
            }

            if (!isMouse || EnableMouseTouchPoint || EnableMouseGestures)
            {
                // Add the new touch event keeping the list from getting
                // too large if no one happens to be requesting the state.
                var evt = new TouchLocationData(touchId, state, position * _touchScale, CurrentTimestamp);

                if (!isMouse || EnableMouseTouchPoint)
                {
                    ApplyTouch(_touchState, evt);
                }

                //If we have gestures enabled then collect events for those too.
                //We also have to keep tracking any touches while we know about touches so we don't miss releases even if gesture recognition is disabled
                if ((EnabledGestures != GestureType.None || _gestureState.Count > 0) && (!isMouse || EnableMouseGestures))
                {
                    ApplyTouch(_gestureState, evt);

                    if (EnabledGestures != GestureType.None)
                        UpdateGestures(true);

                    AgeTouches(_gestureState);
                }
            }

            // If this is a release unmap the hardware id.
            if (state == TouchLocationState.Released)
                _touchIds.Remove(id);
        }

        private void UpdateTouchScale()
        {
            // Get the window size.
            var windowSize = new Vector2(Window.ClientBounds.Width, Window.ClientBounds.Height);

            // Recalculate the touch scale.
            _touchScale = new Vector2(  _displaySize.X / windowSize.X,
                                        _displaySize.Y / windowSize.Y);
        }

        /// <summary>
        /// This will release all touch locations.  It should only be 
        /// called on platforms where touch state is reset all at once.
        /// </summary>
        internal void ReleaseAllTouches()
        {
            var mostToRemove = Math.Max(_touchState.Count, _gestureState.Count);
            if (mostToRemove > 0)
            {
                var temp = new List<TouchLocationData>(mostToRemove);

                // Submit a new event for each non-released location.
                temp.AddRange(_touchState);
                foreach (var touch in temp)
                {
                    if (touch.State != TouchLocationState.Released)
                        ApplyTouch(_touchState, new TouchLocationData(touch.Id, TouchLocationState.Released, touch.Position, CurrentTimestamp));
                }

                temp.Clear();
                temp.AddRange(_gestureState);
                foreach (var touch in temp)
                {
                    if (touch.State != TouchLocationState.Released)
                        ApplyTouch(_gestureState, new TouchLocationData(touch.Id, TouchLocationState.Released, touch.Position, CurrentTimestamp));
                }
            }

            // Release all the touch id mappings.
            _touchIds.Clear();
        }

        /// <summary>
        /// Gets or sets the display height of the touch panel.
        /// </summary>
        public int DisplayHeight
        {
            get
            {
                return _displaySize.Y;
            }
            set
            {
                _displaySize.Y = value;
                UpdateTouchScale();
            }
        }

        /// <summary>
        /// Gets or sets the display orientation of the touch panel.
        /// </summary>
        public DisplayOrientation DisplayOrientation { get; set; }

        /// <summary>
        /// Gets or sets the display width of the touch panel.
        /// </summary>
        public int DisplayWidth
        {
            get
            {
                return _displaySize.X;
            }
            set
            {
                _displaySize.X = value;
                UpdateTouchScale();
            }
        }

        /// <summary>
        /// Gets or sets enabled gestures.
        /// </summary>
        public GestureType EnabledGestures { get; set; }

        public bool EnableMouseTouchPoint { get; set; }

        public bool EnableMouseGestures { get; set; }

        /// <summary>
        /// Returns true if a touch gesture is available.
        /// </summary>
        public bool IsGestureAvailable
        {
            get
            {
                // Process the pending gesture events. (May cause hold events)
                UpdateGestures(false);

                return GestureList.Count > 0;
            }
        }

        /// <summary>
        /// Returns the next available gesture on touch panel device.
        /// </summary>
        /// <returns><see cref="GestureSample"/></returns>
        public GestureSample ReadGesture()
        {
            // Return the next gesture.
            return GestureList.Dequeue();
        }

        #region Gesture Recognition

        /// <summary>
        /// Maximum distance a touch location can wiggle and 
        /// not be considered to have moved.
        /// </summary>
        internal const float TapJitterTolerance = 35.0f;

        internal static readonly TimeSpan TimeRequiredForHold = TimeSpan.FromMilliseconds(1024);

        /// <summary>
        /// The pinch touch locations.
        /// </summary>
        private readonly TouchLocationData[] _pinchTouch = new TouchLocationData[2];

        /// <summary>
        /// If true the pinch touch locations are valid and
        /// a pinch gesture has begun.
        /// </summary>
        private bool _pinchGestureStarted;

        private bool GestureIsEnabled(GestureType gestureType)
        {
            return (EnabledGestures & gestureType) != 0;
        }

        /// <summary>
        /// Used to disable emitting of tap gestures.
        /// </summary>
        bool _tapDisabled;

        /// <summary>
        /// Used to disable emitting of hold gestures.
        /// </summary>
        bool _holdDisabled;


        private void UpdateGestures(bool stateChanged)
        {
            // These are observed XNA gesture rules which we follow below.  Please
            // add to them if a new case is found.
            //
            //  - Tap occurs on release.
            //  - DoubleTap occurs on the first press after a Tap.
            //  - Tap, Double Tap, and Hold are disabled if a drag begins or more than one finger is pressed.
            //  - Drag occurs when one finger is down and actively moving.
            //  - Pinch occurs if 2 or more fingers are down and at least one is moving.
            //  - If you enter a Pinch during a drag a DragComplete is fired.
            //  - Drags are classified as horizontal, vertical, free, or none and stay that way.
            //

            // First get a count of touch locations which 
            // are not in the released state.
            var heldLocations = 0;
            foreach (var touch in _gestureState)
                heldLocations += touch.State != TouchLocationState.Released ? 1 : 0;

            // As soon as we have more than one held point then 
            // tap and hold gestures are disabled until all the 
            // points are released.
            if (heldLocations > 1)
            {
                _tapDisabled = true;
                _holdDisabled = true;
            }

            // Process the touch locations for gestures.
            for (int i = 0; i < _gestureState.Count; i++)
            {
                var touch = _gestureState[i];
                switch (touch.State)
                {
                    case TouchLocationState.Pressed:
                    case TouchLocationState.Moved:
                        {
                            // The DoubleTap event is emitted on first press as
                            // opposed to Tap which happens on release.
                            if (touch.State == TouchLocationState.Pressed &&
                                ProcessDoubleTap(ref touch))
                                break;

                            // Any time more than one finger is down and pinch is
                            // enabled then we exclusively do pinch processing.
                            if (GestureIsEnabled(GestureType.Pinch) && heldLocations > 1)
                            {
                                // Save or update the first pinch point.
                                if (_pinchTouch[0].State == TouchLocationState.Invalid ||
                                        _pinchTouch[0].Id == touch.Id)
                                    _pinchTouch[0] = touch;

                                // Save or update the second pinch point.
                                else if (_pinchTouch[1].State == TouchLocationState.Invalid ||
                                            _pinchTouch[1].Id == touch.Id)
                                    _pinchTouch[1] = touch;

                                // NOTE: Actual pinch processing happens outside and
                                // below this loop to ensure both points are updated
                                // before gestures are emitted.
                                break;
                            }

                            // If we're not dragging try to process a hold event.
                            var sqDist = Vector2.DistanceSquared(touch.Position, touch.PressPosition);
                            if (_dragGestureStarted == GestureType.None && sqDist < TapJitterTolerance * TapJitterTolerance)
                            {
                                ProcessHold(ref touch);
                                break;
                            }

                            // If the touch state has changed then do a drag gesture.
                            if (stateChanged)
                                ProcessDrag(ref touch);
                            break;
                        }

                    case TouchLocationState.Released:
                        {
                            // If the touch state hasn't changed then this
                            // is an old release event... skip it.
                            if (!stateChanged)
                                break;

                            // If this is one of the pinch locations then we
                            // need to fire off the complete event and stop
                            // the pinch gesture operation.
                            if (_pinchGestureStarted &&
                                    (touch.Id == _pinchTouch[0].Id ||
                                        touch.Id == _pinchTouch[1].Id))
                            {
                                if (GestureIsEnabled(GestureType.PinchComplete))
                                    GestureList.Enqueue(new GestureSample(
                                                            GestureType.PinchComplete, touch.Timestamp,
                                                            Vector2.Zero, Vector2.Zero,
                                                            Vector2.Zero, Vector2.Zero));

                                _pinchGestureStarted = false;
                                _pinchTouch[0] = TouchLocationData.Invalid;
                                _pinchTouch[1] = TouchLocationData.Invalid;
                                break;
                            }

                            // If there are still other pressed locations then there
                            // is nothing more we can do with this release.
                            if (heldLocations != 0)
                                break;

                            // From testing XNA it seems we need a velocity 
                            // of about 100 to classify this as a flick.
                            var sqDist = Vector2.DistanceSquared(touch.Position, touch.PressPosition);
                            if (sqDist > TapJitterTolerance * TapJitterTolerance &&
                                    touch.Velocity.LengthSquared() > 100.0f * 100.0f &&
                                    GestureIsEnabled(GestureType.Flick))
                            {
                                GestureList.Enqueue(new GestureSample(
                                                        GestureType.Flick, touch.Timestamp,
                                                        Vector2.Zero, Vector2.Zero,
                                                        touch.Velocity, Vector2.Zero));

                                //fall through, a drag should still happen even if a flick does
                            }

                            // If a drag is active then we need to finalize it.
                            if (_dragGestureStarted != GestureType.None)
                            {
                                if (GestureIsEnabled(GestureType.DragComplete))
                                    GestureList.Enqueue(new GestureSample(
                                                            GestureType.DragComplete, touch.Timestamp,
                                                            Vector2.Zero, Vector2.Zero,
                                                            Vector2.Zero, Vector2.Zero));

                                _dragGestureStarted = GestureType.None;
                                break;
                            }

                            // If all else fails try to process it as a tap.
                            ProcessTap(ref touch);
                            break;
                        }
                }
            }

            // If the touch state hasn't changed then there is no 
            // cleanup to do and no pinch to process.
            if (!stateChanged)
                return;

            // If we have two pinch points then update the pinch state.
            if (GestureIsEnabled(GestureType.Pinch) &&
                    _pinchTouch[0].State != TouchLocationState.Invalid &&
                    _pinchTouch[1].State != TouchLocationState.Invalid)
                ProcessPinch(_pinchTouch);
            else
            {
                // Make sure a partial pinch state 
                // is not left hanging around.
                _pinchGestureStarted = false;
                _pinchTouch[0] = TouchLocationData.Invalid;
                _pinchTouch[1] = TouchLocationData.Invalid;
            }

            // If all points are released then clear some states.
            if (heldLocations == 0)
            {
                _tapDisabled = false;
                _holdDisabled = false;
                _dragGestureStarted = GestureType.None;
            }
        }

        private void ProcessHold(ref TouchLocationData touch)
        {
            if (!GestureIsEnabled(GestureType.Hold) || _holdDisabled)
                return;

            var elapsed = CurrentTimestamp - touch.PressTimestamp;
            if (elapsed < TimeRequiredForHold)
                return;

            _holdDisabled = true;

            GestureList.Enqueue(
                new GestureSample(GestureType.Hold,
                                    touch.Timestamp,
                                    touch.Position, Vector2.Zero,
                                    Vector2.Zero, Vector2.Zero));
        }

        private bool ProcessDoubleTap(ref TouchLocationData touch)
        {
            if (!GestureIsEnabled(GestureType.DoubleTap) || _tapDisabled || _lastTap.State == TouchLocationState.Invalid)
                return false;

            // If the new tap is too far away from the last then
            // this cannot be a double tap event.
            var sqDist = Vector2.DistanceSquared(touch.Position, _lastTap.Position);
            if (sqDist > TapJitterTolerance * TapJitterTolerance)
                return false;

            // Check that this tap happened within the standard 
            // double tap time threshold of 300 milliseconds.
            var elapsed = touch.Timestamp - _lastTap.Timestamp;
            if (elapsed.TotalMilliseconds > 300)
                return false;

            GestureList.Enqueue(new GestureSample(
                           GestureType.DoubleTap, touch.Timestamp,
                           touch.Position, Vector2.Zero,
                           Vector2.Zero, Vector2.Zero));

            // Disable taps until after the next release.
            _tapDisabled = true;

            return true;
        }

        private TouchLocationData _lastTap;

        private void ProcessTap(ref TouchLocationData touch)
        {
            if (_tapDisabled)
                return;

            // If the release is too far away from the press 
            // position then this cannot be a tap event.
            var sqDist = Vector2.DistanceSquared(touch.PressPosition, touch.Position);
            if (sqDist > TapJitterTolerance * TapJitterTolerance)
                return;

            // If we pressed and held too long then don't 
            // generate a tap event for it.
            var elapsed = CurrentTimestamp - touch.PressTimestamp;
            if (elapsed > TimeRequiredForHold)
                return;

            // Store the last tap for 
            // double tap processing.
            _lastTap = touch;

            // Fire off the tap event immediately.
            if (GestureIsEnabled(GestureType.Tap))
            {
                var tap = new GestureSample(
                    GestureType.Tap, touch.Timestamp,
                    touch.Position, Vector2.Zero,
                    Vector2.Zero, Vector2.Zero);
                GestureList.Enqueue(tap);
            }
        }

        private GestureType _dragGestureStarted = GestureType.None;

        private void ProcessDrag(ref TouchLocationData touch)
        {
            var dragH = GestureIsEnabled(GestureType.HorizontalDrag);
            var dragV = GestureIsEnabled(GestureType.VerticalDrag);
            var dragF = GestureIsEnabled(GestureType.FreeDrag);

            if (!dragH && !dragV && !dragF)
                return;

            // Make sure this is a move event and that we have
            // a previous touch location.
            TouchLocationData prevTouch;
            if (touch.State != TouchLocationState.Moved || !touch.TryGetPreviousLocationData(out prevTouch))
                return;

            var delta = touch.Position - prevTouch.Position;

            // If we're free dragging then stick to it.
            if (_dragGestureStarted != GestureType.FreeDrag)
            {
                var isHorizontalDelta = Math.Abs(delta.X) > Math.Abs(delta.Y * 2.0f);
                var isVerticalDelta = Math.Abs(delta.Y) > Math.Abs(delta.X * 2.0f);
                var classify = _dragGestureStarted == GestureType.None;

                // Once we enter either vertical or horizontal drags
                // we stick to it... regardless of the delta.
                if (dragH && ((classify && isHorizontalDelta) || _dragGestureStarted == GestureType.HorizontalDrag))
                {
                    delta.Y = 0;
                    _dragGestureStarted = GestureType.HorizontalDrag;
                }
                else if (dragV && ((classify && isVerticalDelta) || _dragGestureStarted == GestureType.VerticalDrag))
                {
                    delta.X = 0;
                    _dragGestureStarted = GestureType.VerticalDrag;
                }

                // If the delta isn't either horizontal or vertical
                //then it could be a free drag if not classified.
                else if (dragF && classify)
                {
                    _dragGestureStarted = GestureType.FreeDrag;
                }
                else
                {
                    // If we couldn't classify the drag then
                    // it is nothing... set it to complete.
                    _dragGestureStarted = GestureType.DragComplete;
                }
            }

            // If the drag could not be classified then no gesture.
            if (_dragGestureStarted == GestureType.None || _dragGestureStarted == GestureType.DragComplete)
                return;

            _tapDisabled = true;
            _holdDisabled = true;

            GestureList.Enqueue(new GestureSample(
                                    _dragGestureStarted, touch.Timestamp,
                                    touch.Position, Vector2.Zero,
                                    delta, Vector2.Zero));
        }

        private void ProcessPinch(TouchLocationData[] touches)
        {
            TouchLocationData prevPos0;
            TouchLocationData prevPos1;

            if (!touches[0].TryGetPreviousLocationData(out prevPos0))
                prevPos0 = touches[0];

            if (!touches[1].TryGetPreviousLocationData(out prevPos1))
                prevPos1 = touches[1];

            var delta0 = touches[0].Position - prevPos0.Position;
            var delta1 = touches[1].Position - prevPos1.Position;

            // Get the newest timestamp.
            var timestamp = touches[0].Timestamp > touches[1].Timestamp ? touches[0].Timestamp : touches[1].Timestamp;

            // If we were already in a drag state then fire
            // off the drag completion event.
            if (_dragGestureStarted != GestureType.None)
            {
                if (GestureIsEnabled(GestureType.DragComplete))
                    GestureList.Enqueue(new GestureSample(
                                            GestureType.DragComplete, timestamp,
                                            Vector2.Zero, Vector2.Zero,
                                            Vector2.Zero, Vector2.Zero));

                _dragGestureStarted = GestureType.None;
            }

            GestureList.Enqueue(new GestureSample(
                GestureType.Pinch,
                timestamp,
                touches[0].Position, touches[1].Position,
                delta0, delta1));

            _pinchGestureStarted = true;
            _tapDisabled = true;
            _holdDisabled = true;
        }

        #endregion


        internal struct TouchLocationData : IEquatable<TouchLocationData>
        {
            /// <summary>
            ///Attributes 
            /// </summary>
            private int _id;
            private TouchLocationState _state;
            private Vector2 _position;
            private Vector2 _previousPosition;
            private TouchLocationState _previousState;

            // Used for gesture recognition.
            private Vector2 _velocity;
            private Vector2 _pressPosition;
            private TimeSpan _pressTimestamp;
            private TimeSpan _timestamp;

            /// <summary>
            /// True if this touch was pressed and released on the same frame.
            /// In this case we will keep it around for the user to get by GetState that frame.
            /// However if they do not call GetState that frame, this touch will be forgotten.
            /// </summary>
            internal bool SameFrameReleased;

            /// <summary>
            /// Helper for assigning an invalid touch location.
            /// </summary>
            internal static readonly TouchLocationData Invalid = new TouchLocationData();

            #region Properties

            internal Vector2 PressPosition { get { return _pressPosition; } }

            internal TimeSpan PressTimestamp { get { return _pressTimestamp; } }

            internal TimeSpan Timestamp { get { return _timestamp; } }

            internal Vector2 Velocity { get { return _velocity; } }

            internal int Id { get { return _id; } }

            internal Vector2 Position { get { return _position; } }

            internal TouchLocationState State { get { return _state; } }

            internal TouchLocation TouchLocation 
            { 
                get
                {
                    return new TouchLocation(this._id,
                                             this._state, this._position,
                                             this._previousState, this._previousPosition);
                }
            }

            #endregion

            #region Constructors

            internal TouchLocationData(int id, TouchLocationState state, Vector2 position, TimeSpan timestamp)
            {
                _id = id;
                _state = state;
                _position = position;

                _previousState = TouchLocationState.Invalid;
                _previousPosition = Vector2.Zero;

                _timestamp = timestamp;
                _velocity = Vector2.Zero;

                // If this is a pressed location then store the 
                // current position and timestamp as pressed.
                if (state == TouchLocationState.Pressed)
                {
                    _pressPosition = _position;
                    _pressTimestamp = _timestamp;
                }
                else
                {
                    _pressPosition = Vector2.Zero;
                    _pressTimestamp = TimeSpan.Zero;
                }

                SameFrameReleased = false;
            }

            #endregion

            /// <summary>
            /// Returns a copy of the touch with the state changed to moved.
            /// </summary>
            /// <returns>The new touch location.</returns>
            internal TouchLocationData AsMovedState()
            {
                var touch = this;

                // Store the current state as the previous.
                touch._previousState = touch._state;
                touch._previousPosition = touch._position;

                // Set the new state.
                touch._state = TouchLocationState.Moved;

                return touch;
            }

            /// <summary>
            /// Updates the touch location using the new event.
            /// </summary>
            /// <param name="touchEvent">The next event for this touch location.</param>
            internal bool UpdateState(TouchLocationData touchEvent)
            {
                System.Diagnostics.Debug.Assert(Id == touchEvent.Id, "The touch event must have the same Id!");
                System.Diagnostics.Debug.Assert(State != TouchLocationState.Released, "We shouldn't be changing state on a released location!");
                System.Diagnostics.Debug.Assert(touchEvent.State == TouchLocationState.Moved ||
                                touchEvent.State == TouchLocationState.Released, "The new touch event should be a move or a release!");
                System.Diagnostics.Debug.Assert(touchEvent.Timestamp >= _timestamp, "The touch event is older than our timestamp!");

                // Store the current state as the previous one.
                _previousPosition = _position;
                _previousState = _state;

                // Set the new state.
                _position = touchEvent._position;
                if (touchEvent.State == TouchLocationState.Released)
                    _state = touchEvent._state;

                // If time has elapsed then update the velocity.
                var delta = _position - _previousPosition;
                var elapsed = touchEvent.Timestamp - _timestamp;
                if (elapsed > TimeSpan.Zero)
                {
                    // Use a simple low pass filter to accumulate velocity.
                    var velocity = delta / (float)elapsed.TotalSeconds;
                    _velocity += (velocity - _velocity) * 0.45f;
                }

                //Going straight from pressed to released on the same frame
                if (_previousState == TouchLocationState.Pressed && _state == TouchLocationState.Released && elapsed == TimeSpan.Zero)
                {
                    //Lie that we are pressed for now
                    SameFrameReleased = true;
                    _state = TouchLocationState.Pressed;
                }

                // Set the new timestamp.
                _timestamp = touchEvent.Timestamp;

                // Return true if the state actually changed.
                return _state != _previousState || delta.LengthSquared() > 0.001f;
            }

            public override bool Equals(object obj)
            {
                if (obj is TouchLocationData)
                    return Equals((TouchLocationData)obj);

                return false;
            }

            public bool Equals(TouchLocationData other)
            {
                return _id.Equals(other._id) &&
                        _position.Equals(other._position) &&
                        _previousPosition.Equals(other._previousPosition);
            }

            public override int GetHashCode()
            {
                return _id;
            }

            public override string ToString()
            {
                return "Touch id:" + _id + " state:" + _state + " position:" + _position + " pressure:" + 0f + " prevState:" + _previousState + " prevPosition:" + _previousPosition + " previousPressure:" + 0f;
            }

            public bool TryGetPreviousLocationData(out TouchLocationData aPreviousLocation)
            {
                if (_previousState == TouchLocationState.Invalid)
                {
                    aPreviousLocation._id = -1;
                    aPreviousLocation._state = TouchLocationState.Invalid;
                    aPreviousLocation._position = Vector2.Zero;
                    aPreviousLocation._previousState = TouchLocationState.Invalid;
                    aPreviousLocation._previousPosition = Vector2.Zero;
                    aPreviousLocation._timestamp = TimeSpan.Zero;
                    aPreviousLocation._pressPosition = Vector2.Zero;
                    aPreviousLocation._pressTimestamp = TimeSpan.Zero;
                    aPreviousLocation._velocity = Vector2.Zero;
                    aPreviousLocation.SameFrameReleased = false;
                    return false;
                }

                aPreviousLocation._id = _id;
                aPreviousLocation._state = _previousState;
                aPreviousLocation._position = _previousPosition;
                aPreviousLocation._previousState = TouchLocationState.Invalid;
                aPreviousLocation._previousPosition = Vector2.Zero;
                aPreviousLocation._timestamp = _timestamp;
                aPreviousLocation._pressPosition = _pressPosition;
                aPreviousLocation._pressTimestamp = _pressTimestamp;
                aPreviousLocation._velocity = _velocity;
                aPreviousLocation.SameFrameReleased = SameFrameReleased;
                return true;
            }

            public static bool operator !=(TouchLocationData value1, TouchLocationData value2)
            {
                return value1._id != value2._id ||
                        value1._state != value2._state ||
                        value1._position != value2._position ||
                        value1._previousState != value2._previousState ||
                        value1._previousPosition != value2._previousPosition;
            }

            public static bool operator ==(TouchLocationData value1, TouchLocationData value2)
            {
                return value1._id == value2._id &&
                        value1._state == value2._state &&
                        value1._position == value2._position &&
                        value1._previousState == value2._previousState &&
                        value1._previousPosition == value2._previousPosition;
            }


            internal void AgeState()
            {
                if (_state == TouchLocationState.Moved)
                {
                    _previousState = _state;
                    _previousPosition = _position;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(_state == TouchLocationState.Pressed, "Can only age the state of touches that are in the Pressed State");

                    _previousState = _state;
                    _previousPosition = _position;

                    if (SameFrameReleased)
                        _state = TouchLocationState.Released;
                    else
                        _state = TouchLocationState.Moved;
                }
            }

        }

    }
}
