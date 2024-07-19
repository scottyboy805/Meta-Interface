using System;
using UnityEngine;

namespace UltimateReplay.Formatters
{
    public sealed class ReplayAnimatorFormatter : ReplayFormatter
    {
        // Types
        /// <summary>
        /// Serialize flags used to indicate which data elements are stored.
        /// </summary>
        [Flags]
        public enum ReplayAnimatorSerializeFlags : byte
        {
            /// <summary>
            /// The main state layer data will be serialized.
            /// </summary>
            MainState = 1 << 1,
            /// <summary>
            /// Sub state layers will be serialized.
            /// </summary>
            SubStates = 1 << 2,
            /// <summary>
            /// Parameter values will be serialized.
            /// </summary>
            Parameters = 1 << 3,

            IKPosition = 1 << 4,
            IKRotation = 1 << 5,
            IKWeights = 1 << 6,

            /// <summary>
            /// Supported data elements will be serialized using low precision mode.
            /// </summary>
            LowPrecision = 1 << 7,
        }

        /// <summary>
        /// Contains data about a specific animator state.
        /// </summary>
        public struct ReplayAnimatorState
        {
            // Public
            /// <summary>
            /// The hash of the current animator state.
            /// </summary>
            public int stateHash;
            /// <summary>
            /// The normalized playback time of the current animation.
            /// </summary>
            public float normalizedTime;
            /// <summary>
            /// The current speed of the animation.
            /// </summary>
            public float speed;
            /// <summary>
            /// The current speed multiplier value.
            /// </summary>
            public float speedMultiplier;
        }

        /// <summary>
        /// Contains data about a specific animator parameter.
        /// </summary>
        public struct ReplayAnimatorParameter
        {
            // Public
            /// <summary>
            /// The name hash of the parameter.
            /// </summary>
            public int nameHash;
            /// <summary>
            /// The <see cref="AnimatorControllerParameterType"/> which describes the type of parameter.
            /// </summary>
            public AnimatorControllerParameterType parameterType;
            /// <summary>
            /// The integer value of the parameter.
            /// </summary>
            public int intValue;
            /// <summary>
            /// The float value of the parameter.
            /// </summary>
            public float floatValue;
            /// <summary>
            /// The bool value of the parameter.
            /// </summary>
            public bool boolValue;
        }

        /// <summary>
        /// Contains data about a specific animator IK limb.
        /// </summary>
        public struct ReplayAnimatorIKTarget
        {
            // Public
            /// <summary>
            /// The target position for the IK limb.
            /// </summary>
            public Vector3 targetPosition;
            /// <summary>
            /// The target rotation for the IK limb.
            /// </summary>
            public Quaternion targetRotation;
            /// <summary>
            /// The position weight for the IK limb.
            /// </summary>
            public float positionWeight;
            /// <summary>
            /// The rotation weight for the IK limb.
            /// </summary>
            public float rotationWeight;
        }

        // Private
        private ReplayAnimatorSerializeFlags serializeFlags = 0;
        private ReplayAnimatorState[] states = new ReplayAnimatorState[0];
        private ReplayAnimatorParameter[] parameters = new ReplayAnimatorParameter[0];
        private ReplayAnimatorIKTarget[] ikTargets = new ReplayAnimatorIKTarget[IKLimbCount];

        // Public
        public const int IKLimbCount = 4;

        // Properties
        internal ReplayAnimatorSerializeFlags SerializeFlags
        {
            get { return serializeFlags; }
        }

        public bool ReplayParameters
        {
            get { return (serializeFlags & ReplayAnimatorSerializeFlags.Parameters) != 0; }
            set
            {
                // Clear bit
                serializeFlags &= ~ReplayAnimatorSerializeFlags.Parameters;

                // Set bit
                if (value == true) serializeFlags |= ReplayAnimatorSerializeFlags.Parameters;
            }
        }

        public bool LowPrecision
        {
            get { return (serializeFlags & ReplayAnimatorSerializeFlags.LowPrecision) != 0; }
            set
            {
                // Clear bit
                serializeFlags &= ~ReplayAnimatorSerializeFlags.LowPrecision;

                // Set bit
                if (value == true) serializeFlags |= ReplayAnimatorSerializeFlags.LowPrecision;
            }
        }

        /// <summary>
        /// Get the <see cref="ReplayAnimatorState"/> information for the main state.
        /// </summary>
        public ReplayAnimatorState MainState
        {
            get
            {
                if (states.Length == 0)
                    return new ReplayAnimatorState();

                return states[0];
            }
            set
            {
                if (states.Length == 0)
                    states = new ReplayAnimatorState[1];

                states[0] = value;
            }
        }

        /// <summary>
        /// Get all <see cref="ReplayAnimatorState"/> information for all sub states.
        /// </summary>
        public ReplayAnimatorState[] States
        {
            get { return states; }
            set
            {
                states = value;

                if (states == null)
                    states = new ReplayAnimatorState[0];
            }
        }

        /// <summary>
        /// Get all <see cref="ReplayAnimatorParameter"/> that will be serialized.
        /// </summary>
        public ReplayAnimatorParameter[] Parameters
        {
            get { return parameters; }
            set
            {
                parameters = value;

                if (parameters == null)
                    parameters = new ReplayAnimatorParameter[0];
            }
        }

        /// <summary>
        /// Get all <see cref="ReplayAnimatorIKTarget"/> that will be serialized.
        /// </summary>
        public ReplayAnimatorIKTarget[] IKTargets
        {
            get { return ikTargets; }
            set
            {
                ikTargets = value;

                if (ikTargets == null || ikTargets.Length != IKLimbCount)
                    ikTargets = new ReplayAnimatorIKTarget[IKLimbCount];
            }
        }

        // Constructor
        public ReplayAnimatorFormatter()
        {
            for (int i = 0; i < ikTargets.Length; i++)
            {
                // Set to identity to avoid scalar assertion
                ikTargets[i].targetRotation = Quaternion.identity;
            }
        }

        // Methods
        /// <summary>
        /// Invoke this method to serialize the animator data to the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The state object used to store the data</param>
        public override void OnReplaySerialize(ReplayState state)
        {
            // Write flags
            state.Write((byte)serializeFlags);

            // Check if main or sub states are recorded
            if((serializeFlags & ReplayAnimatorSerializeFlags.MainState) != 0 || (serializeFlags & ReplayAnimatorSerializeFlags.SubStates) != 0)
            {
                // Gte the number of states
                int writeStatesCount = states.Length;

                // Write states
                state.Write((byte)writeStatesCount);

                // Write all states
                for(int i = 0; i < writeStatesCount; i++)
                {
                    // Get the state data
                    ReplayAnimatorState animState = states[i];

                    // Hash cannot be low precision
                    state.Write(animState.stateHash);

                    // Check for low precision time values
                    if((serializeFlags & ReplayAnimatorSerializeFlags.LowPrecision) != 0)
                    {
                        // Write half precision
                        state.WriteHalf(animState.normalizedTime);
                    }
                    else
                    {
                        // Write full precision
                        state.Write(animState.normalizedTime);
                    }
                }
            }

            // Write parameters
            if((serializeFlags & ReplayAnimatorSerializeFlags.Parameters) != 0)
            {
                state.Write((byte)parameters.Length);

                // Write all parameters
                for(int i = 0; i < parameters.Length; i++)
                {
                    // Get the parameter data
                    ReplayAnimatorParameter animParam = parameters[i];

                    // Write the name hash
                    state.Write(animParam.nameHash);

                    // Write the type
                    state.Write((byte)animParam.parameterType);

                    // Write by parameter type
                    switch (animParam.parameterType)
                    {
                        case AnimatorControllerParameterType.Bool:
                        case AnimatorControllerParameterType.Trigger:
                            {
                                state.Write(animParam.boolValue);
                                break;
                            }

                        case AnimatorControllerParameterType.Float:
                            {
                                if ((serializeFlags & ReplayAnimatorSerializeFlags.LowPrecision) != 0)
                                {
                                    // Write half precision
                                    state.WriteHalf(animParam.floatValue);
                                }
                                else
                                {
                                    // Write full precision
                                    state.Write(animParam.floatValue);
                                }
                                break;
                            }

                        case AnimatorControllerParameterType.Int:
                            {
                                state.Write(animParam.intValue);
                                break;
                            }
                    }
                }
            } // End parameters


            // Write IK target positions
            if((serializeFlags & ReplayAnimatorSerializeFlags.IKPosition) != 0)
            {
                for(int i = 0; i < ikTargets.Length; i++)
                {
                    // Check for low precision
                    if((serializeFlags & ReplayAnimatorSerializeFlags.LowPrecision) != 0)
                    {
                        // Write position as half precision
                        state.WriteHalf(ikTargets[i].targetPosition);
                    }
                    else
                    {
                        // Write position
                        state.Write(ikTargets[i].targetPosition);
                    }
                }
            }

            // Write IK target rotations
            if((serializeFlags & ReplayAnimatorSerializeFlags.IKRotation) != 0)
            {
                for(int i = 0; i < ikTargets.Length; i++)
                {
                    // Check for low precision
                    if((serializeFlags & ReplayAnimatorSerializeFlags.LowPrecision) != 0)
                    {
                        // Write rotation as half precision
                        state.WriteHalf(ikTargets[i].targetRotation);
                    }
                    else
                    {
                        // Write rotation
                        state.Write(ikTargets[i].targetRotation);
                    }
                }
            }

            // Write IK weights
            if((serializeFlags & ReplayAnimatorSerializeFlags.IKWeights) != 0)
            {
                for(int i = 0; i < ikTargets.Length; i++)
                {
                    // Check for low precision
                    if((serializeFlags & ReplayAnimatorSerializeFlags.LowPrecision) != 0)
                    {
                        // Write weights as low precision
                        state.WriteHalf(ikTargets[i].positionWeight);
                        state.WriteHalf(ikTargets[i].rotationWeight);
                    }
                    else
                    {
                        // Write weights
                        state.Write(ikTargets[i].positionWeight);
                        state.Write(ikTargets[i].rotationWeight);
                    }
                }
            } // End IK
        }

        /// <summary>
        /// Invoke this method to deserialize the animator data from the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The state object which should contain valid animator data</param>
        public override void OnReplayDeserialize(ReplayState state)
        {
            // Read flags
            serializeFlags = (ReplayAnimatorSerializeFlags)state.ReadByte();

            // Check for any states recorded
            if ((serializeFlags & ReplayAnimatorSerializeFlags.MainState) != 0 || (serializeFlags & ReplayAnimatorSerializeFlags.SubStates) != 0)
            {
                // Read the number of states
                int readStateCount = state.ReadByte();

                // Allocate arrays
                states = new ReplayAnimatorState[readStateCount];

                // Read all states
                for(int i = 0; i < readStateCount; i++)
                {
                    // Create the state data - Quicker than calling ctor
                    ReplayAnimatorState animState = default;

                    // Read hash
                    animState.stateHash = state.ReadInt32();

                    // Check for low precision time values
                    if((serializeFlags & ReplayAnimatorSerializeFlags.LowPrecision) != 0)
                    {
                        // Read half precision
                        animState.normalizedTime = state.ReadHalf();
                    }
                    else
                    {
                        // Read full precision
                        animState.normalizedTime = state.ReadSingle();
                    }

                    // Fill element
                    states[i] = animState;
                }
            }

            // Read parameters
            if ((serializeFlags & ReplayAnimatorSerializeFlags.Parameters) != 0)
            {
                // Read the number of parameters
                int readParamsCount = state.ReadByte();

                // Check if we need to allocate the array
                if (parameters.Length != readParamsCount)
                {
                    // Resize our array
                    Array.Resize(ref parameters, readParamsCount);
                }

                // Read all parameters
                for(int i = 0; i < readParamsCount; i++)
                {
                    // Create the parameter data - Quicker than calling ctor
                    ReplayAnimatorParameter animParam = default;

                    // Read the name hash
                    animParam.nameHash = state.ReadInt32();

                    // Read the type
                    animParam.parameterType = (AnimatorControllerParameterType)state.ReadByte();

                    switch (animParam.parameterType)
                    {
                        case AnimatorControllerParameterType.Bool:
                        case AnimatorControllerParameterType.Trigger:
                            {
                                animParam.boolValue = state.ReadBool();
                                break;
                            }

                        case AnimatorControllerParameterType.Float:
                            {
                                if ((serializeFlags & ReplayAnimatorSerializeFlags.LowPrecision) != 0)
                                {
                                    // Read half precision
                                    animParam.floatValue = state.ReadHalf();
                                }
                                else
                                {
                                    // Read full precision
                                    animParam.floatValue = state.ReadSingle();
                                }
                                break;
                            }

                        case AnimatorControllerParameterType.Int:
                            {
                                animParam.intValue = state.ReadInt32();
                                break;
                            }
                    }

                    // Fill array
                    parameters[i] = animParam;
                }
            }

            // Check for any IK recorded and allocate new arrays
            if ((serializeFlags & (ReplayAnimatorSerializeFlags.IKPosition | ReplayAnimatorSerializeFlags.IKRotation | ReplayAnimatorSerializeFlags.IKWeights)) != 0)
                ikTargets = new ReplayAnimatorIKTarget[ikTargets.Length];

            // Read IK target positions
            if ((serializeFlags & ReplayAnimatorSerializeFlags.IKPosition) != 0)
            {
                for (int i = 0; i < ikTargets.Length; i++)
                {
                    // Check for low precision
                    if ((serializeFlags & ReplayAnimatorSerializeFlags.LowPrecision) != 0)
                    {
                        // Write position as half precision
                        ikTargets[i].targetPosition = state.ReadVector3Half();
                    }
                    else
                    {
                        // Write position
                        ikTargets[i].targetPosition = state.ReadVector3();
                    }
                }
            }

            // Read IK target rotations
            if ((serializeFlags & ReplayAnimatorSerializeFlags.IKRotation) != 0)
            {
                for (int i = 0; i < ikTargets.Length; i++)
                {
                    // Check for low precision
                    if ((serializeFlags & ReplayAnimatorSerializeFlags.LowPrecision) != 0)
                    {
                        // Write rotation as half precision
                        ikTargets[i].targetRotation = state.ReadQuaternionHalf();
                    }
                    else
                    {
                        // Write rotation
                        ikTargets[i].targetRotation= state.ReadQuaternion();
                    }
                }
            }

            // Read IK weights
            if ((serializeFlags & ReplayAnimatorSerializeFlags.IKWeights) != 0)
            {
                for (int i = 0; i < ikTargets.Length; i++)
                {
                    // Check for low precision
                    if ((serializeFlags & ReplayAnimatorSerializeFlags.LowPrecision) != 0)
                    {
                        // Write weights as low precision
                        ikTargets[i].positionWeight = state.ReadHalf();
                        ikTargets[i].rotationWeight = state.ReadHalf();
                    }
                    else
                    {
                        // Write weights
                        ikTargets[i].positionWeight = state.ReadSingle();
                        ikTargets[i].rotationWeight = state.ReadSingle();
                    }
                }
            } // End IK
        }

        public ReplayAnimatorIKTarget GetIKTargetInfo(AvatarIKGoal goal)
        {
            return ikTargets[(int)goal];
        }

        public void SetIKTargetInfo(AvatarIKGoal goal, in ReplayAnimatorIKTarget target)
        {
            ikTargets[(int)(goal)] = target;
        }

        internal void SyncAnimator(Animator sync, AnimatorControllerParameter[] animParams, ReplayAnimatorSerializeFlags flags)
        {
            // Play main state
            if (states.Length > 0)
                sync.Play(states[0].stateHash, 0, states[0].normalizedTime);

            // Play sub states
            if((flags & ReplayAnimatorSerializeFlags.SubStates) != 0 && states.Length > 1)
            {
                for(int i = 1; i < states.Length; i++)
                {
                    sync.Play(states[i].stateHash, i, states[i].normalizedTime);
                }
            }

            // Update parameters
            if((flags & ReplayAnimatorSerializeFlags.Parameters) != 0 && animParams != null)
            {
                for(int i = 0; i < animParams.Length && i < parameters.Length; i++)
                {
                    // Get the parameter
                    ReplayAnimatorParameter param = parameters[i];

                    switch(param.parameterType)
                    {
                        case AnimatorControllerParameterType.Bool:
                            {
                                sync.SetBool(param.nameHash, param.boolValue);
                                break;
                            }

                        case AnimatorControllerParameterType.Int:
                            {
                                sync.SetInteger(param.nameHash, param.intValue);
                                break;
                            }

                        case AnimatorControllerParameterType.Float:
                            {
                                sync.SetFloat(param.nameHash, param.floatValue);
                                break;
                            }
                    }
                }
            }
        }

        internal void UpdateFromAnimator(Animator anim, AnimatorControllerParameter[] animParams, ReplayAnimatorSerializeFlags flags)
        {
            // Store flags
            this.serializeFlags = flags;

            // Update states
            int statesCount = ((flags & ReplayAnimatorSerializeFlags.SubStates) != 0) ? anim.layerCount : 1;

            // Resize arrays
            if (statesCount != states.Length)
                Array.Resize(ref states, statesCount);

            // Record animator states
            for (int i = 0; i < statesCount; i++)
            {
                // Get the observed animator state info
                AnimatorStateInfo animState = anim.GetCurrentAnimatorStateInfo(i);

                // Create the layer state info
                states[i] = new ReplayAnimatorState
                {
                    stateHash = animState.fullPathHash,
                    normalizedTime = animState.normalizedTime,
                    speed = animState.speed,
                    speedMultiplier = animState.speedMultiplier,
                };
            }

            // Record animator parameters
            if ((flags & ReplayAnimatorSerializeFlags.Parameters) != 0)
            {
                int paramsCount = animParams.Length;

                // Resize arrays
                if (paramsCount != parameters.Length)
                    Array.Resize(ref parameters, paramsCount);

                // Record animator parameters
                for (int i = 0; i < paramsCount; i++)
                {
                    // Get the observed animator parameter info
                    AnimatorControllerParameter animParam = animParams[i];

                    switch (animParam.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            {
                                parameters[i] = new ReplayAnimatorParameter
                                {
                                    nameHash = animParam.nameHash,
                                    parameterType = AnimatorControllerParameterType.Bool,
                                    boolValue = anim.GetBool(animParam.name),
                                };
                                break;
                            }

                        case AnimatorControllerParameterType.Trigger:
                            {
                                parameters[i] = new ReplayAnimatorParameter
                                {
                                    nameHash = animParam.nameHash,
                                    parameterType = AnimatorControllerParameterType.Trigger,
                                    boolValue = anim.GetBool(animParam.name),
                                };
                                break;
                            }

                        case AnimatorControllerParameterType.Int:
                            {
                                parameters[i] = new ReplayAnimatorParameter
                                {
                                    nameHash = animParam.nameHash,
                                    parameterType = AnimatorControllerParameterType.Int,
                                    intValue = anim.GetInteger(animParam.name),
                                };
                                break;
                            }

                        case AnimatorControllerParameterType.Float:
                            {
                                parameters[i] = new ReplayAnimatorParameter
                                {
                                    nameHash = animParam.nameHash,
                                    parameterType = AnimatorControllerParameterType.Float,
                                    floatValue = anim.GetFloat(animParam.name),
                                };
                                break;
                            }
                    }
                }
            }
        }

        internal void UpdateIKFromAnimator(Animator anim, ReplayAnimatorSerializeFlags flags)
        {
            // Record IK position
            if ((flags & ReplayAnimatorSerializeFlags.IKPosition) != 0)
            {
                ikTargets[(int)AvatarIKGoal.LeftFoot].targetPosition = anim.GetIKPosition(AvatarIKGoal.LeftFoot);
                ikTargets[(int)AvatarIKGoal.RightFoot].targetPosition = anim.GetIKPosition(AvatarIKGoal.RightFoot);
                ikTargets[(int)AvatarIKGoal.LeftHand].targetPosition = anim.GetIKPosition(AvatarIKGoal.LeftHand);
                ikTargets[(int)AvatarIKGoal.RightHand].targetPosition = anim.GetIKPosition(AvatarIKGoal.RightHand);
            }

            // Record IK rotation
            if ((flags & ReplayAnimatorSerializeFlags.IKRotation) != 0)
            {
                ikTargets[(int)AvatarIKGoal.LeftFoot].targetRotation = anim.GetIKRotation(AvatarIKGoal.LeftFoot);
                ikTargets[(int)AvatarIKGoal.RightFoot].targetRotation = anim.GetIKRotation(AvatarIKGoal.RightFoot);
                ikTargets[(int)AvatarIKGoal.LeftHand].targetRotation = anim.GetIKRotation(AvatarIKGoal.LeftHand);
                ikTargets[(int)AvatarIKGoal.RightHand].targetRotation = anim.GetIKRotation(AvatarIKGoal.RightHand);
            }

            // Record IK weights
            if ((flags & ReplayAnimatorSerializeFlags.IKWeights) != 0)
            {
                ikTargets[(int)AvatarIKGoal.LeftFoot].positionWeight = anim.GetIKPositionWeight(AvatarIKGoal.LeftFoot);
                ikTargets[(int)AvatarIKGoal.LeftFoot].rotationWeight = anim.GetIKRotationWeight(AvatarIKGoal.LeftFoot);

                ikTargets[(int)AvatarIKGoal.RightFoot].positionWeight = anim.GetIKPositionWeight(AvatarIKGoal.RightFoot);
                ikTargets[(int)AvatarIKGoal.RightFoot].rotationWeight = anim.GetIKRotationWeight(AvatarIKGoal.RightFoot);

                ikTargets[(int)AvatarIKGoal.LeftHand].positionWeight = anim.GetIKPositionWeight(AvatarIKGoal.LeftHand);
                ikTargets[(int)AvatarIKGoal.LeftHand].rotationWeight = anim.GetIKRotationWeight(AvatarIKGoal.LeftHand);

                ikTargets[(int)AvatarIKGoal.RightHand].positionWeight = anim.GetIKPositionWeight(AvatarIKGoal.RightHand);
                ikTargets[(int)AvatarIKGoal.RightHand].rotationWeight = anim.GetIKRotationWeight(AvatarIKGoal.RightHand);
            }
        }

        internal static ReplayAnimatorSerializeFlags GetSerializeFlags(bool recordParameters, bool ikPosition, bool ikRotation, bool ikWeights, RecordPrecision precision)
        {
            ReplayAnimatorSerializeFlags flags = ReplayAnimatorSerializeFlags.MainState | ReplayAnimatorSerializeFlags.SubStates;

            // Parameters
            if(recordParameters == true) flags |= ReplayAnimatorSerializeFlags.Parameters;

            // IK
            if (ikPosition == true) flags |= ReplayAnimatorSerializeFlags.IKPosition;
            if (ikRotation == true) flags |= ReplayAnimatorSerializeFlags.IKRotation;
            if (ikWeights == true) flags |= ReplayAnimatorSerializeFlags.IKWeights;

            // Low precision
            if ((precision & RecordPrecision.HalfPrecision16Bit) != 0) flags |= ReplayAnimatorSerializeFlags.LowPrecision;

            return flags;
        }
    }
}
