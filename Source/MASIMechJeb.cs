﻿/*****************************************************************************
 * The MIT License (MIT)
 * 
 * Copyright (c) 2016-2018 MOARdV
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 * 
 ****************************************************************************/
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AvionicsSystems
{
    /// <summary>
    /// MASIMechJeb is the interface to MechJeb.
    /// </summary>
    /// <LuaName>mechjeb</LuaName>
    /// <mdDoc>MASIMechJeb provides the interface for controlling and querying
    /// MechJeb from Avionics Systems.</mdDoc>
    internal class MASIMechJeb
    {
        private static readonly bool mjFound;

        //--- Methods found in MechJebCore
        private static readonly Type mjCore_t;

        //--- Methods found in AbsoluteVector
        private static readonly FieldInfo AbsoluteVectorLat;
        private static readonly FieldInfo AbsoluteVectorLon;

        //--- Methods found in ComputerModule
        private static readonly Func<object, bool> ModuleEnabled;
        private static readonly FieldInfo ModuleUsers;
        private static readonly FieldInfo Target;

        //--- Methods found in EditableDoubleMult
        private static readonly Func<object, double, object> setEditableDoubleMult;
        private static readonly Func<object, double> getEditableDoubleMult;

        //--- Methods found in OrbitalManeuverCalculator
        // Actually, this requires 5 parameters, with the 5 an 'out' parameter.
        private static readonly DynamicMethodDelegate<object> DeltaVAndTimeForInterplanetaryTransferEjection;
        // The 4th parameter is an 'out' parameter.
        private static readonly DynamicMethodDelegate<object> DeltaVAndTimeForHohmannTransfer;
        private static readonly Func<Orbit, double, double, Vector3d> DeltaVToChangeApoapsis;
        private static readonly Func<Orbit, double, double, Vector3d> DeltaVToChangePeriapsis;
        private static readonly Func<Orbit, double, Vector3d> DeltaVToCircularize;
        private static readonly Func<Orbit, double, Orbit, Vector3d> DeltaVToMatchVelocities;

        //--- Methods found in VesselExtensions
        private static readonly Type mjVesselExtensions_t;
        private static readonly Func<Vessel, object> GetMasterMechJeb;
        private static readonly Func<object, string, object> GetComputerModule;
        private static readonly Func<Vessel, Orbit, Vector3d, double, object> PlaceManeuverNode;

        //--- Methods found in MechJebModuleAscentAutopilot
        private static readonly Func<object, bool> GetAPForceRoll;
        private static readonly Func<object, object> GetLaunchAltitude;
        private static readonly Func<object, double> GetLaunchInclination;
        private static readonly Func<object, object> GetAPTurnRoll;
        private static readonly Func<object, object> GetAPVerticalRoll;
        private static readonly Action<object, bool> SetForceRoll;
        private static readonly Action<object, double> SetLaunchInclination;

        //--- Methods found in ModuleAscentGuidance
        private static readonly Func<object, object> GetInclinationAG;

        //--- Methods found in ModuleLandingAutopilot
        private static readonly Func<object, object, object> LandAtPositionTarget;
        private static readonly Func<object, object, object> LandUntargeted;
        private static readonly Func<object, object> StopLanding;

        //--- Methods found in ModuleLandingPredictions
        private static readonly Func<object, object> GetPredictionsResult;

        //--- Methods found in ModuleNodeExecutor
        private static readonly Func<object, object> AbortNode;
        private static readonly Func<object, object, object> ExecuteOneNode;

        //--- Methods found in ModuleSmartASS
        private static readonly FieldInfo saTarget_t;
        private static readonly Func<object, bool, object> Engage;
        internal static readonly string[] modeNames;

        //--- Methods found in ModuleTargetController
        private static readonly Func<object, bool> PositionTargetExists;

        //--- Methods found in ReentrySimulation.Result
        private static readonly FieldInfo ReentryEndPosition;
        private static readonly FieldInfo ReentryOutcome;
        private static readonly FieldInfo ReentryTime;

        //--- Methods found in StageStats
        private static readonly Func<object, object, bool, object> RequestUpdate;
        private static readonly FieldInfo AtmoStats;
        private static readonly FieldInfo VacStats;
        private static readonly Func<object, int> GetStatsLength;
        private static readonly Func<object, int, object> GetStatsIndex;

        //--- Field in FuelFlowSimulation.Stats
        private static readonly FieldInfo StatsStageDv;
        private static readonly Func<object, object> GetStageDv;

        //--- Methods found in UserPool
        private static readonly Func<object, object, object> AddUser;
        private static readonly Func<object, object, object> RemoveUser;

        //--- Instance variables
        internal bool mjAvailable;

        Vessel vessel;
        MASVesselComputer vc;
        Orbit vesselOrbit;

        bool landingPredictionEnabled;
        double landingAltitude;
        double landingLatitude;
        double landingLongitude;
        double landingTime;

        double deltaV;
        double deltaVStage;

        object masterMechJeb;
        object ascentAutopilot;
        object ascentGuidance;
        object landingAutopilot;
        object landingGuidance;
        object landingPrediction;
        object maneuverPlanner;
        object nodeExecutor;
        object rendezvousAutopilot;
        object rendezvousAutopilotWindow;
        object smartAss;
        object stageStats;

        private SATarget saTarget;

        #region MechJeb Enum Imports
        public enum SATarget
        {
            OFF = 0,
            KILLROT = 1,
            NODE = 2,
            SURFACE = 3,
            PROGRADE = 4,
            RETROGRADE = 5,
            NORMAL_PLUS = 6,
            NORMAL_MINUS = 7,
            RADIAL_PLUS = 8,
            RADIAL_MINUS = 9,
            RELATIVE_PLUS = 10,
            RELATIVE_MINUS = 11,
            TARGET_PLUS = 12,
            TARGET_MINUS = 13,
            PARALLEL_PLUS = 14,
            PARALLEL_MINUS = 15,
            ADVANCED = 16,
            AUTO = 17,
            SURFACE_PROGRADE = 18,
            SURFACE_RETROGRADE = 19,
            HORIZONTAL_PLUS = 20,
            HORIZONTAL_MINUS = 21,
            VERTICAL_PLUS = 22,
        }
        static private Dictionary<int, SATarget> saTargetMap = new Dictionary<int, SATarget>
        {
            { 0, SATarget.OFF },
            { 1, SATarget.KILLROT },
            { 2,SATarget.NODE },
            { 3,SATarget.SURFACE },
            { 4,SATarget.PROGRADE },
            { 5,SATarget.RETROGRADE },
            { 6,SATarget.NORMAL_PLUS },
            { 7,SATarget.NORMAL_MINUS },
            { 8,SATarget.RADIAL_PLUS },
            { 9,SATarget.RADIAL_MINUS },
            {10,SATarget.RELATIVE_PLUS },
            {11,SATarget.RELATIVE_MINUS },
            {12,SATarget.TARGET_PLUS },
            {13,SATarget.TARGET_MINUS },
            {14,SATarget.PARALLEL_PLUS },
            {15,SATarget.PARALLEL_MINUS },
            {16,SATarget.ADVANCED },
            {17,SATarget.AUTO },
            {18,SATarget.SURFACE_PROGRADE },
            {19,SATarget.SURFACE_RETROGRADE },
            {20,SATarget.HORIZONTAL_PLUS },
            {21,SATarget.HORIZONTAL_MINUS },
            {22,SATarget.VERTICAL_PLUS }
        };
        #endregion

        [MoonSharpHidden]
        public MASIMechJeb()
        {
            mjAvailable = mjFound;
        }

        ~MASIMechJeb()
        {
            vesselOrbit = null;
        }

        /// <summary>
        /// The General category provides some non-specific queries and
        /// actions (such as whether
        /// MechJeb functionality is available on a given craft).
        /// </summary>
        #region MechJeb General
        /// <summary>
        /// Returns 1 if any of the MechJeb autopilots MAS can control are active.
        /// </summary>
        /// <returns></returns>
        public double AutopilotActive()
        {
            if (mjAvailable && (ModuleEnabled(ascentAutopilot) || ModuleEnabled(landingAutopilot) || ModuleEnabled(nodeExecutor) || ModuleEnabled(rendezvousAutopilot)))
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Returns 1 if MechJeb is installed and available on this craft, 0 if it
        /// is not available.
        /// </summary>
        /// <returns></returns>
        public double Available()
        {
            return (mjAvailable) ? 1.0 : 0.0;
        }

        /// <summary>
        /// Returns 1 if any managed autopilots are engaged, or if SmartASS is not OFF.
        /// </summary>
        /// <returns></returns>
        public double ComputerActive()
        {
            if (mjAvailable && (saTarget != SATarget.OFF || ModuleEnabled(ascentAutopilot) || ModuleEnabled(landingAutopilot) || ModuleEnabled(nodeExecutor) || ModuleEnabled(rendezvousAutopilot)))
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Resets / disables all auto pilots as well as SmartASS.
        /// </summary>
        public void Reset()
        {
            if (mjAvailable)
            {
                if (saTarget != SATarget.OFF)
                {
                    saTarget_t.SetValue(smartAss, 0);

                    Engage(smartAss, true);
                }

                if (ModuleEnabled(ascentAutopilot))
                {
                    RemoveUser(ModuleUsers.GetValue(ascentAutopilot), ascentGuidance);
                }

                if (ModuleEnabled(landingAutopilot))
                {
                    StopLanding(landingAutopilot);
                }

                if (ModuleEnabled(nodeExecutor))
                {
                    AbortNode(nodeExecutor);
                }

                if (ModuleEnabled(rendezvousAutopilot))
                {
                    RemoveUser(ModuleUsers.GetValue(rendezvousAutopilot), rendezvousAutopilotWindow);
                }
            }
        }
        #endregion

        /// <summary>
        /// The Ascent Autopilot and Guidance methods provide an interface to MechJeb's
        /// Ascent Autopilot.
        /// </summary>
        #region MechJeb Ascent Autopilot and Guidance

        /// <summary>
        /// Returns 1 if the MJ Ascent Autopilot is enabled; 0 otherwise.
        /// </summary>
        /// <returns></returns>
        public double AscentAutopilotActive()
        {
            if (mjAvailable && ModuleEnabled(ascentAutopilot))
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Returns the MJ Ascent Autopilot's targeted launch altitude in meters.
        /// </summary>
        /// <returns></returns>
        public double GetDesiredLaunchAltitude()
        {
            double desiredAltitude = 0.0;
            if (mjAvailable)
            {
                object desiredAlt = GetLaunchAltitude(ascentAutopilot);
                if (desiredAlt != null)
                {
                    desiredAltitude = getEditableDoubleMult(desiredAlt);
                }
            }

            return desiredAltitude;
        }

        /// <summary>
        /// Returns the MJ Ascent Autopilot's targeted orbital inclination in degrees.
        /// </summary>
        /// <returns></returns>
        public double GetDesiredLaunchInclination()
        {
            double desiredInclination = 0.0;
            if (mjAvailable)
            {
                return GetLaunchInclination(ascentAutopilot);
                //object desiredInc = desiredOrbitInclination_t.GetValue(ascentAutopilot);
                //desiredInclination = (double)desiredInc;
            }

            return desiredInclination;
        }

        /// <summary>
        /// Returns 1 if MechJeb is installed and the ascent autopilot's Force Roll mode is
        /// enabled.  Returns 0 otherwise.
        /// </summary>
        /// <returns></returns>
        public double GetForceRoll()
        {
            if (mjAvailable)
            {
                return GetAPForceRoll(ascentAutopilot) ? 1.0 : 0.0;
            }
            return 0.0;
        }

        /// <summary>
        /// Return the current ascent autopilot turn roll.
        /// </summary>
        /// <returns>Current turn roll setting.</returns>
        public double GetTurnRoll()
        {
            if (mjAvailable)
            {
                object turnRoll = GetAPTurnRoll(ascentAutopilot);
                if (turnRoll != null)
                {
                    return getEditableDoubleMult(turnRoll);
                }
            }
            return 0.0;
        }

        /// <summary>
        /// Return the current ascent autopilot vertical roll.
        /// </summary>
        /// <returns>Current vertical roll setting.</returns>
        public double GetVerticalRoll()
        {
            if (mjAvailable)
            {
                object vertRoll = GetAPVerticalRoll(ascentAutopilot);
                if (vertRoll != null)
                {
                    return getEditableDoubleMult(vertRoll);
                }
            }
            return 0.0;
        }

        /// <summary>
        /// Set the ascent guidance desired altitude.  Altitude is in meters.
        /// </summary>
        /// <param name="altitude"></param>
        public void SetDesiredLaunchAltitude(double altitude)
        {
            if (mjAvailable)
            {
                object desiredAlt = GetLaunchAltitude(ascentAutopilot);
                if (desiredAlt != null)
                {
                    setEditableDoubleMult(desiredAlt, altitude);
                }
            }
        }

        /// <summary>
        /// Set the desired launch inclination in the ascent guidance.
        /// </summary>
        /// <param name="inclination"></param>
        public void SetDesiredLaunchInclination(double inclination)
        {
            if (mjAvailable)
            {
                SetLaunchInclination(ascentAutopilot, inclination);
                // Just writing it to the autopilot doesn't update the ascent guidance gui...
                object desiredInc = GetInclinationAG(ascentGuidance);
                if (desiredInc != null)
                {
                    setEditableDoubleMult(desiredInc, inclination);
                }
            }
        }

        /// <summary>
        /// Set the ascent autopilot's turn roll to the value specified.
        /// </summary>
        /// <param name="turnRoll">The turn roll, in degrees.</param>
        /// <returns>1 if the value was set, 0 otherwise.</returns>
        public double SetTurnRoll(double turnRoll)
        {
            if (mjAvailable)
            {
                object turnRollED = GetAPTurnRoll(ascentAutopilot);
                if (turnRollED != null)
                {
                    turnRoll = Utility.NormalizeLongitude(turnRoll);
                    setEditableDoubleMult(turnRollED, turnRoll);
                    return 1.0;
                }
            }
            return 0.0;
        }

        /// <summary>
        /// Set the ascent autopilot's vertical roll to the value specified.
        /// </summary>
        /// <param name="turnRoll">The vertical roll, in degrees.</param>
        /// <returns>1 if the value was set, 0 otherwise.</returns>
        public double SetVerticalRoll(double verticalRoll)
        {
            if (mjAvailable)
            {
                object vertRollED = GetAPVerticalRoll(ascentAutopilot);
                if (vertRollED != null)
                {
                    verticalRoll = Utility.NormalizeLongitude(verticalRoll);
                    setEditableDoubleMult(vertRollED, verticalRoll);
                    return 1.0;
                }
            }
            return 0.0;
        }

        /// <summary>
        /// Toggles the Ascent Autopilot on or off.
        /// </summary>
        /// <returns>1 if the autopilot was engaged, 0 if it was disengaged or unavailable.</returns>
        public double ToggleAscentAutopilot()
        {
            if (mjAvailable)
            {
                object users = ModuleUsers.GetValue(ascentAutopilot);
                if (users == null)
                {
                    Utility.LogWarning(this, "ascentAutopilot's ModuleUsers is null");
                }

                if (ModuleEnabled(ascentAutopilot))
                {
                    RemoveUser(users, ascentGuidance);
                }
                else
                {
                    AddUser(users, ascentGuidance);
                    return 1.0;
                }
            }

            return 0.0;
        }

        /// <summary>
        /// Toggle the Ascent Autopilot's "Force Roll" option.
        /// </summary>
        /// <returns></returns>
        public double ToggleForceRoll()
        {
            if (mjAvailable)
            {
                SetForceRoll(ascentAutopilot, !GetAPForceRoll(ascentAutopilot));
                return 1.0;
            }

            return 0.0;
        }
        #endregion

        /// <summary>
        /// The Landing Autopilot and Computer methods provide an interface to MechJeb's
        /// Landing Autopilot.  It can also independently trigger the Landing Computer to
        /// provide landing predictions.
        /// </summary>
        #region MechJeb Landing Autopilot and Computer
        /// <summary>
        /// Returns 1 if the MechJeb landing autopilot is engaged.
        /// </summary>
        /// <returns>1 if the landing autopilot is active, 0 otherwise</returns>
        public double LandingAutopilotActive()
        {
            if (mjAvailable && ModuleEnabled(landingAutopilot))
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Returns 1 if the MechJeb landing prediction computer is engaged.
        /// </summary>
        /// <returns>1 if the landing prediction computer is enabled; 0 otherwise.</returns>
        public double LandingComputerActive()
        {
            if (mjAvailable && landingPredictionEnabled)
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// When the landing prediction computer is engaged, returns the
        /// predicted altitude of the landing site.  Returns 0 otherwise.
        /// </summary>
        /// <returns>Terrain Altitude in meters at the predicted landing
        /// site; 0 if the orbit does not land.</returns>
        public double LandingAltitude()
        {
            if (mjAvailable && landingPredictionEnabled)
            {
                return landingAltitude;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// When the landing prediction computer is engaged, returns the
        /// predicted latitude of the landing site.  Returns 0 otherwise.
        /// </summary>
        /// <returns>Latitude in degrees; north is positive, south is negative; 0
        /// if the prediciton computer is off or the orbit does not land.</returns>
        public double LandingLatitude()
        {
            if (mjAvailable && landingPredictionEnabled)
            {
                return landingLatitude;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// When the landing prediction computer is engaged, returns the
        /// predicted longitude of the landing site.  Returns 0 otherwise.
        /// </summary>
        /// <returns>Longitude in degrees.  West is negative, east is positive; 0 if the orbit
        /// will not land.</returns>
        public double LandingLongitude()
        {
            if (mjAvailable && landingPredictionEnabled)
            {
                return landingLongitude;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// When the prediction computer is engaged, returns the predicted time until
        /// landing.
        /// </summary>
        /// <returns>Time in seconds until landing; 0 if the prediction computer is off or the orbit does not land.</returns>
        public double LandingTime()
        {
            if (mjAvailable && landingPredictionEnabled)
            {
                return landingTime;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Toggles the MechJeb landing autopilot on/off.
        /// </summary>
        /// <returns>1 if the autopilot was switched on, 0 if it was switched off.</returns>
        public double ToggleLandingAutopilot()
        {
            if (mjAvailable)
            {
                if (ModuleEnabled(landingAutopilot))
                {
                    StopLanding(landingAutopilot);
                }
                else
                {
                    object target = Target.GetValue(masterMechJeb);
                    if (PositionTargetExists(target))
                    {
                        LandAtPositionTarget(landingAutopilot, landingGuidance);
                    }
                    else
                    {
                        LandUntargeted(landingAutopilot, landingGuidance);
                    }
                    return 1.0;
                }
            }

            return 0.0;
        }

        /// <summary>
        /// Toggles the MechJeb landing prediction computer on/off independently
        /// of the landing autopilot.
        /// </summary>
        /// <returns>1 if the prediction computer was switched on, 0 if it was switched off.</returns>
        public double ToggleLandingComputer()
        {
            if (mjAvailable)
            {
                object users = ModuleUsers.GetValue(landingPrediction);

                if (ModuleEnabled(landingPrediction))
                {
                    RemoveUser(users, landingGuidance);
                }
                else
                {
                    AddUser(users, landingGuidance);

                    return 1.0;
                }
            }

            return 0.0;
        }
        #endregion

        /// <summary>
        /// The Maneuver Planner and Node Executor section provides access to the MechJeb autopilot
        /// and maneuver node planner.
        /// </summary>
        #region MechJeb Maneuver Planner and Node Executor
        /// <summary>
        /// Change apoapsis to the specified altitude in meters.  Command is
        /// ignored if Ap &lt; Pe or the orbit is hyperbolic and the vessel is
        /// already past the Pe.
        /// </summary>
        /// <param name="newAp">The new apoapsis in meters.</param>
        /// <returns>1 on success, 0 on failure</returns>
        public double ChangeApoapsis(double newAp)
        {
            if (mjAvailable && newAp >= vesselOrbit.PeA && vessel.patchedConicSolver != null)
            {
                double nextPeriapsisTime = vesselOrbit.timeToPe;
                if (nextPeriapsisTime > 0.0)
                {
                    vessel.patchedConicSolver.maneuverNodes.Clear();

                    nextPeriapsisTime += Planetarium.GetUniversalTime();
                    Vector3d dV = DeltaVToChangeApoapsis(vesselOrbit, nextPeriapsisTime, vesselOrbit.referenceBody.Radius + newAp);

                    PlaceManeuverNode(vessel, vesselOrbit, dV, nextPeriapsisTime);

                    return 1.0;
                }
            }

            return 0.0;
        }

        /// <summary>
        /// Change Periapsis to the new altitude in meters.  Command is ignored
        /// if Pe &gt; Ap.
        /// </summary>
        /// <param name="newPe">The new periapsis in meters.</param>
        /// <returns>1 on success, 0 on failure</returns>
        public double ChangePeriapsis(double newPe)
        {
            if (mjAvailable && vesselOrbit.eccentricity < 1.0 && newPe <= vesselOrbit.ApA && vessel.patchedConicSolver != null)
            {
                double nextApoapsisTime = vesselOrbit.timeToAp;
                if (nextApoapsisTime > 0.0)
                {
                    vessel.patchedConicSolver.maneuverNodes.Clear();

                    nextApoapsisTime += Planetarium.GetUniversalTime();
                    Vector3d dV = DeltaVToChangePeriapsis(vesselOrbit, nextApoapsisTime, vesselOrbit.referenceBody.Radius + newPe);

                    PlaceManeuverNode(vessel, vesselOrbit, dV, nextApoapsisTime);

                    return 1.0;
                }
            }

            return 0.0;
        }

        /// <summary>
        /// Circularize at the specified altitude, in meters.  Command is
        /// ignored if an invalid altitude is supplied.
        /// </summary>
        /// <param name="newAlt">The altitude to circularize the orbit at, in meters.</param>
        /// <returns>1 on success, 0 on failure</returns>
        public double CircularizeAt(double newAlt)
        {
            if (mjAvailable && newAlt >= vesselOrbit.PeA && newAlt <= vesselOrbit.ApA && vessel.patchedConicSolver != null)
            {
                vessel.patchedConicSolver.maneuverNodes.Clear();

                double tA = vesselOrbit.TrueAnomalyAtRadius(newAlt + vesselOrbit.referenceBody.Radius);
                double nextAltTime = vesselOrbit.GetUTforTrueAnomaly(tA, vesselOrbit.period);

                Vector3d dV = DeltaVToCircularize(vesselOrbit, nextAltTime);
                PlaceManeuverNode(vessel, vesselOrbit, dV, nextAltTime);

                return 1.0;
            }

            return 0.0;
        }

        /// <summary>
        /// Returns 1 if the maneuver node executor is active, 0 otherwise.
        /// </summary>
        /// <returns>1 if the node exeuctor is enabled; 0 otherwise.</returns>
        public double ManeuverNodeExecutorActive()
        {
            if (mjAvailable && ModuleEnabled(nodeExecutor))
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Instructs MechJeb to match velocities with the target at
        /// closest approach.
        /// </summary>
        /// <returns>1 on success, 0 on failure</returns>
        public double MatchVelocities()
        {
            if (vc.activeTarget != null && vc.activeTarget.GetOrbit() != null && vessel.patchedConicSolver != null)
            {
                Vector3d dV = DeltaVToMatchVelocities(vesselOrbit, vc.targetClosestUT, vc.activeTarget.GetOrbit());
                vessel.patchedConicSolver.maneuverNodes.Clear();
                PlaceManeuverNode(vessel, vesselOrbit, dV, vc.targetClosestUT);

                return 1.0;
            }

            return 0.0;
        }

        /// <summary>
        /// When a target is selected and the following conditions are met, this
        /// method will instruct MechJeb to plot an optimized Hohmann transfer
        /// to intercept the target.
        /// 
        /// If the target orbits a different body, an interplanetary transfer is
        /// calculated.
        /// </summary>
        /// <returns>1 on success, 0 on failure</returns>
        public double PlotTransfer()
        {
            if (vc.activeTarget != null)
            {
                Orbit targetOrbit = vc.activeTarget.GetOrbit();
                if (targetOrbit != null)
                {
                    try
                    {
                        double nodeUT = vc.universalTime;
                        Vector3d dV;
                        if (targetOrbit.referenceBody == vesselOrbit.referenceBody)
                        {
                            object[] args = new object[] { vesselOrbit, targetOrbit, vc.universalTime, nodeUT };
                            dV = (Vector3d)DeltaVAndTimeForHohmannTransfer(null, args);
                            nodeUT = (double)args[3];
                        }
                        else
                        {
                            object[] args = new object[] { vesselOrbit, vc.universalTime, targetOrbit, true, nodeUT };
                            dV = (Vector3d)DeltaVAndTimeForInterplanetaryTransferEjection(null, args);
                            nodeUT = (double)args[4];
                        }
                        vessel.patchedConicSolver.maneuverNodes.Clear();
                        PlaceManeuverNode(vessel, vesselOrbit, dV, nodeUT);

                        return 1.0;
                    }
                    catch { }
                }
            }

            return 0.0;
        }

        /// <summary>
        /// Enables / disables Maneuver Node Executor
        /// </summary>
        /// <returns>1 if the node exeuctor is enabled; 0 otherwise.</returns>
        public double ToggleManeuverNodeExecutor()
        {
            if (mjAvailable)
            {
                if (ModuleEnabled(nodeExecutor))
                {
                    AbortNode(nodeExecutor);
                }
                else
                {
                    ExecuteOneNode(nodeExecutor, maneuverPlanner);
                    return 1.0;
                }
            }

            return 0.0;
        }
        #endregion

        /// <summary>
        /// The Performance section provides metrics of vessel performance as computed my MechJeb.
        /// </summary>
        #region MechJeb Performance

        /// <summary>
        /// Returns the dV remaining for the vessel.
        /// </summary>
        /// <returns>dV in m/s.</returns>
        public double DeltaV()
        {
            return deltaV;
        }

        /// <summary>
        /// Returns the dV remaining for the currently active stage.
        /// </summary>
        /// <returns>dV in m/s.</returns>
        public double StageDeltaV()
        {
            return deltaVStage;
        }
        #endregion

        /// <summary>
        /// The Rendezvous Autopilot category contains methods to interact with the Rendezvous
        /// Autopilot.
        /// </summary>
        #region MechJeb Rendezvous Autopilot
        /// <summary>
        /// Returns 1 if the rendezvous autopilot is engaged.
        /// </summary>
        /// <returns></returns>
        public double RendezvousAutopilotActive()
        {
            if (mjAvailable && ModuleEnabled(rendezvousAutopilot))
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Engages / disengages the MechJeb Rendezvous Autopilot
        /// </summary>
        /// <returns>1 if the autopilot was switched on, 0 if it was switched off.</returns>
        public double ToggleRendezvousAutopilot()
        {
            if (mjAvailable)
            {
                object users = ModuleUsers.GetValue(rendezvousAutopilot);
                if (ModuleEnabled(rendezvousAutopilot))
                {
                    RemoveUser(users, rendezvousAutopilotWindow);
                }
                else
                {
                    AddUser(users, rendezvousAutopilotWindow);
                    return 1.0;
                }
            }

            return 0.0;
        }
        #endregion

        /// <summary>
        /// MechJeb's SASS attitude control system can be queried and controlled using
        /// methods from this category.
        /// </summary>
        #region MechJeb SASS
        /// <summary>
        /// Returns the number of the currently active SASS mode, or zero if MechJeb
        /// is unavailable.
        /// 
        /// * OFF = 0,
        /// * KILLROT = 1,
        /// * NODE = 2,
        /// * SURFACE = 3,
        /// * PROGRADE = 4,
        /// * RETROGRADE = 5,
        /// * NORMAL_PLUS = 6,
        /// * NORMAL_MINUS = 7,
        /// * RADIAL_PLUS = 8,
        /// * RADIAL_MINUS = 9,
        /// * RELATIVE_PLUS = 10,
        /// * RELATIVE_MINUS = 11,
        /// * TARGET_PLUS = 12,
        /// * TARGET_MINUS = 13,
        /// * PARALLEL_PLUS = 14,
        /// * PARALLEL_MINUS = 15,
        /// * ADVANCED = 16,
        /// * AUTO = 17,
        /// * SURFACE_PROGRADE = 18,
        /// * SURFACE_RETROGRADE = 19,
        /// * HORIZONTAL_PLUS = 20,
        /// * HORIZONTAL_MINUS = 21,
        /// * VERTICAL_PLUS = 22,
        /// </summary>
        /// <returns></returns>
        public double GetSASSMode()
        {
            if (mjAvailable)
            {
                return (double)(int)saTarget;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Returns 1 if the current SASS mode matches the listed value.
        /// 
        /// * OFF = 0,
        /// * KILLROT = 1,
        /// * NODE = 2,
        /// * SURFACE = 3,
        /// * PROGRADE = 4,
        /// * RETROGRADE = 5,
        /// * NORMAL_PLUS = 6,
        /// * NORMAL_MINUS = 7,
        /// * RADIAL_PLUS = 8,
        /// * RADIAL_MINUS = 9,
        /// * RELATIVE_PLUS = 10,
        /// * RELATIVE_MINUS = 11,
        /// * TARGET_PLUS = 12,
        /// * TARGET_MINUS = 13,
        /// * PARALLEL_PLUS = 14,
        /// * PARALLEL_MINUS = 15,
        /// * ADVANCED = 16,
        /// * AUTO = 17,
        /// * SURFACE_PROGRADE = 18,
        /// * SURFACE_RETROGRADE = 19,
        /// * HORIZONTAL_PLUS = 20,
        /// * HORIZONTAL_MINUS = 21,
        /// * VERTICAL_PLUS = 22,
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public double GetSASSModeActive(double mode)
        {
            int mode_i = (int)mode;
            if (mjAvailable && saTargetMap.ContainsKey(mode_i) && saTarget == saTargetMap[mode_i])
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Returns true if SASS is off
        /// </summary>
        /// <returns></returns>
        public double SASSOff()
        {
            if (mjAvailable && saTarget == SATarget.OFF)
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Set the SASS pilot to the specified mode.  Some modes may not
        /// be 'settable', such as `AUTO` or `ADVANCED`.
        /// 
        /// * OFF = 0,
        /// * KILLROT = 1,
        /// * NODE = 2,
        /// * SURFACE = 3,
        /// * PROGRADE = 4,
        /// * RETROGRADE = 5,
        /// * NORMAL_PLUS = 6,
        /// * NORMAL_MINUS = 7,
        /// * RADIAL_PLUS = 8,
        /// * RADIAL_MINUS = 9,
        /// * RELATIVE_PLUS = 10,
        /// * RELATIVE_MINUS = 11,
        /// * TARGET_PLUS = 12,
        /// * TARGET_MINUS = 13,
        /// * PARALLEL_PLUS = 14,
        /// * PARALLEL_MINUS = 15,
        /// * ADVANCED = 16,
        /// * AUTO = 17,
        /// * SURFACE_PROGRADE = 18,
        /// * SURFACE_RETROGRADE = 19,
        /// * HORIZONTAL_PLUS = 20,
        /// * HORIZONTAL_MINUS = 21,
        /// * VERTICAL_PLUS = 22,
        /// </summary>
        /// <param name="mode"></param>
        public void SetSASSMode(double mode)
        {
            int mode_i = (int)mode;
            if (mjAvailable && saTargetMap.ContainsKey(mode_i))
            {
                saTarget_t.SetValue(smartAss, mode_i);

                Engage(smartAss, true);
            }
        }
        #endregion

        /// <summary>
        /// Do all of the internal processing we need to do per FixedUpdate.
        /// </summary>
        [MoonSharpHidden]
        internal void Update()
        {
            if (mjAvailable)
            {
                object activeSATarget = saTarget_t.GetValue(smartAss);
                saTarget = saTargetMap[(int)activeSATarget];

                landingPredictionEnabled = ModuleEnabled(landingPrediction);

                bool landingPredictionRead = false;
                if (landingPredictionEnabled)
                {
                    object predictions = GetPredictionsResult(landingPrediction);
                    if (predictions != null)
                    {
                        object outcome = ReentryOutcome.GetValue(predictions);
                        if (outcome != null && outcome.ToString() == "LANDED")
                        {
                            object endPosition = ReentryEndPosition.GetValue(predictions);
                            if (endPosition != null)
                            {
                                landingLatitude = (double)AbsoluteVectorLat.GetValue(endPosition);
                                landingLongitude = (double)AbsoluteVectorLon.GetValue(endPosition);
                                landingTime = (double)ReentryTime.GetValue(predictions) - Planetarium.GetUniversalTime();

                                landingAltitude = FinePrint.Utilities.CelestialUtilities.TerrainAltitude(vessel.mainBody, landingLatitude, landingLongitude);
                                landingPredictionRead = true;
                            }
                        }
                    }
                }

                if (!landingPredictionRead)
                {
                    landingAltitude = 0.0;
                    landingLatitude = 0.0;
                    landingLongitude = 0.0;
                    landingTime = 0.0;
                }

                RequestUpdate(stageStats, this, false);
                int atmStatsLength = 0, vacStatsLength = 0;

                object atmStatsO = AtmoStats.GetValue(stageStats);
                object vacStatsO = VacStats.GetValue(stageStats);
                if (atmStatsO != null)
                {
                    atmStatsLength = GetStatsLength(atmStatsO);
                }
                if (vacStatsO != null)
                {
                    vacStatsLength = GetStatsLength(vacStatsO);
                }

                deltaV = deltaVStage = 0.0;

                if (atmStatsLength > 0 && atmStatsLength == vacStatsLength)
                {
                    double atmospheresLocal = vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres;

                    for (int i = 0; i < atmStatsLength; ++i)
                    {
                        object atmStat = GetStatsIndex(atmStatsO, i);
                        object vacStat = GetStatsIndex(vacStatsO, i);
                        if (atmStat == null || vacStat == null)
                        {
                            Utility.LogStaticError("atmStat or vacStat did not evaluate");
                            deltaV = deltaVStage = 0.0;
                            return;
                        }

                        //object a = GetStageDv(atmStat);
                        //Utility.LogMessage(this, "GetStageDv returns {0}", a.GetType().Name); // <- returns Stats, not double?
                        //double atm1 = (double)GetStageDv(atmStat); // These values are wrong
                        //double vac1 = (double)GetStageDv(vacStat);
                        double atm = (double)StatsStageDv.GetValue(atmStat);
                        double vac = (double)StatsStageDv.GetValue(vacStat);
                        double stagedV = UtilMath.LerpUnclamped(vac, atm, atmospheresLocal);
                        //Utility.LogMessage(this, "{0,2}: A = {1:0.0} - V = {2:0.0}, getter says A = {3:0.0} - V = {4:0.0}", i, atm, vac, atm1, vac1);

                        deltaV += stagedV;

                        if (i == (atmStatsLength - 1))
                        {
                            deltaVStage = stagedV;
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Vessel changed - we need to re-establish the master MechJeb and our
        /// delegates.
        /// </summary>
        /// <param name="vessel"></param>
        [MoonSharpHidden]
        internal void UpdateVessel(Vessel vessel, MASVesselComputer vc)
        {
            if (mjFound)
            {
                this.vessel = vessel;
                this.vc = vc;
                this.vesselOrbit = vessel.GetOrbit();
                try
                {
                    masterMechJeb = GetMasterMechJeb(vessel);

                    if (masterMechJeb != null)
                    {
                        smartAss = GetComputerModule(masterMechJeb, "MechJebModuleSmartASS");
                        if (smartAss == null)
                        {
                            throw new Exception("MASIMechJeb: Failed to get SmartASS MJ module");
                        }

                        ascentAutopilot = GetComputerModule(masterMechJeb, "MechJebModuleAscentAutopilot");
                        if (ascentAutopilot == null)
                        {
                            throw new Exception("MASIMechJeb: Failed to get Ascent Autopilot MJ module");
                        }

                        ascentGuidance = GetComputerModule(masterMechJeb, "MechJebModuleAscentGuidance");
                        if (ascentGuidance == null)
                        {
                            throw new Exception("MASIMechJeb: Failed to get Ascent Guidance MJ module");
                        }

                        landingAutopilot = GetComputerModule(masterMechJeb, "MechJebModuleLandingAutopilot");
                        if (landingAutopilot == null)
                        {
                            throw new Exception("MASIMechJeb: Failed to get Landing Autopilot MJ module");
                        }

                        landingGuidance = GetComputerModule(masterMechJeb, "MechJebModuleLandingGuidance");
                        if (landingGuidance == null)
                        {
                            throw new Exception("MASIMechJeb: Failed to get Landing Guidance MJ module");
                        }

                        landingPrediction = GetComputerModule(masterMechJeb, "MechJebModuleLandingPredictions");
                        if (landingPrediction == null)
                        {
                            throw new Exception("MASIMechJeb: Failed to get Landing Prediction MJ module");
                        }

                        maneuverPlanner = GetComputerModule(masterMechJeb, "MechJebModuleManeuverPlanner");
                        if (maneuverPlanner == null)
                        {
                            throw new Exception("MASIMechJeb: Failed to get Maneuver Planner MJ module");
                        }

                        nodeExecutor = GetComputerModule(masterMechJeb, "MechJebModuleNodeExecutor");
                        if (nodeExecutor == null)
                        {
                            throw new Exception("MASIMechJeb: Failed to get Node Executor MJ module");
                        }

                        rendezvousAutopilot = GetComputerModule(masterMechJeb, "MechJebModuleRendezvousAutopilot");
                        if (rendezvousAutopilot == null)
                        {
                            throw new Exception("MASIMechJeb: Failed to get Rendezvous Autopilot MJ module");
                        }

                        rendezvousAutopilotWindow = GetComputerModule(masterMechJeb, "MechJebModuleRendezvousAutopilotWindow");
                        if (rendezvousAutopilotWindow == null)
                        {
                            throw new Exception("MASIMechJeb: Failed to get Rendezvous Autopilot Window MJ module");
                        }

                        stageStats = GetComputerModule(masterMechJeb, "MechJebModuleStageStats");
                        if (stageStats == null)
                        {
                            throw new Exception("MASIMechJeb: Failed to get Stage Stats Window MJ module");
                        }
                    }

                    mjAvailable = (masterMechJeb != null);
                }
                catch (Exception e)
                {
                    Utility.LogError(this, "mechjeb.UpdateVessel threw exception: {0}", e);
                    mjAvailable = false;
                }
            }
        }

        #region Reflection Configuration
        static MASIMechJeb()
        {
            // Spaghetti code: I wanted to use readonly qualifiers on the static
            // variables, but that requires me to do all of this in the static
            // constructor.
            mjFound = false;
            try
            {
                mjCore_t = Utility.GetExportedType("MechJeb2", "MuMech.MechJebCore");
                if (mjCore_t == null)
                {
                    return;
                }
                mjVesselExtensions_t = Utility.GetExportedType("MechJeb2", "MuMech.VesselExtensions");
                if (mjVesselExtensions_t == null)
                {
                    return;
                }
                Type mjComputerModule_t = Utility.GetExportedType("MechJeb2", "MuMech.ComputerModule");
                if (mjComputerModule_t == null)
                {
                    return;
                }
                Type mjEditableDoubleMult_t = Utility.GetExportedType("MechJeb2", "MuMech.EditableDoubleMult");
                if (mjEditableDoubleMult_t == null)
                {
                    return;
                }
                Type mjModuleSmartass_t = Utility.GetExportedType("MechJeb2", "MuMech.MechJebModuleSmartASS");
                if (mjModuleSmartass_t == null)
                {
                    return;
                }
                Type mjModuleAscentAP_t = Utility.GetExportedType("MechJeb2", "MuMech.MechJebModuleAscentAutopilot");
                if (mjModuleAscentAP_t == null)
                {
                    return;
                }
                Type mjModuleAscentGuid_t = Utility.GetExportedType("MechJeb2", "MuMech.MechJebModuleAscentGuidance");
                if (mjModuleAscentGuid_t == null)
                {
                    return;
                }
                Type mjLandingAutopilot_t = Utility.GetExportedType("MechJeb2", "MuMech.MechJebModuleLandingAutopilot");
                if (mjLandingAutopilot_t == null)
                {
                    return;
                }
                Type mjNodeExecutor_t = Utility.GetExportedType("MechJeb2", "MuMech.MechJebModuleNodeExecutor");
                if (mjNodeExecutor_t == null)
                {
                    throw new ArgumentNullException("mjNodeExecutor_t");
                }
                Type mjOrbitalManeuverCalculator_t = Utility.GetExportedType("MechJeb2", "MuMech.OrbitalManeuverCalculator");
                if (mjOrbitalManeuverCalculator_t == null)
                {
                    throw new ArgumentNullException("mjOrbitalManeuverCalculator_t");
                }
                Type mjModuleTargetController_t = Utility.GetExportedType("MechJeb2", "MuMech.MechJebModuleTargetController");
                if (mjModuleTargetController_t == null)
                {
                    throw new ArgumentNullException("mjModuleTargetController_t");
                }
                Type mjUserPool_t = Utility.GetExportedType("MechJeb2", "MuMech.UserPool");
                if (mjUserPool_t == null)
                {
                    throw new ArgumentNullException("mjUserPool_t");
                }
                Type mjModuleStageStats_t = Utility.GetExportedType("MechJeb2", "MuMech.MechJebModuleStageStats");
                if (mjModuleStageStats_t == null)
                {
                    throw new NotImplementedException("mjModuleStageStats_t");
                }
                //---FuelFlowSimulation
                Type mjFuelFlowSimulation_t = Utility.GetExportedType("MechJeb2", "MuMech.FuelFlowSimulation");
                if (mjFuelFlowSimulation_t == null)
                {
                    throw new NotImplementedException("mjFuelFlowSimulation_t");
                }
                Type mjFuelFlowSimulationStats_t = mjFuelFlowSimulation_t.GetNestedType("Stats");
                if (mjFuelFlowSimulationStats_t == null)
                {
                    throw new NotImplementedException("mjFuelFlowSimulationStats_t");
                }

                //--- MechJebCore
                MethodInfo GetComputerModule_t = mjCore_t.GetMethod("GetComputerModule", new Type[] { typeof(string) });
                if (GetComputerModule_t == null)
                {
                    return;
                }
                GetComputerModule = DynamicMethodFactory.CreateDynFunc<object, string, object>(GetComputerModule_t);
                if (GetComputerModule == null)
                {
                    return;
                }
                Target = mjCore_t.GetField("target", BindingFlags.Instance | BindingFlags.Public);
                if (Target == null)
                {
                    throw new ArgumentNullException("Target");
                }

                //--- ComputerModule
                PropertyInfo mjModuleEnabledProperty = mjComputerModule_t.GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
                MethodInfo mjModuleEnabled = null;
                if (mjModuleEnabledProperty != null)
                {
                    mjModuleEnabled = mjModuleEnabledProperty.GetGetMethod();
                }
                if (mjModuleEnabled == null)
                {
                    return;
                }
                ModuleEnabled = DynamicMethodFactory.CreateFunc<object, bool>(mjModuleEnabled);
                ModuleUsers = mjComputerModule_t.GetField("users", BindingFlags.Instance | BindingFlags.Public);
                if (ModuleUsers == null)
                {
                    throw new ArgumentNullException("ModuleUsers");
                }

                //--- EditableDoubleMult
                PropertyInfo edmVal = mjEditableDoubleMult_t.GetProperty("val");
                if (edmVal == null)
                {
                    throw new ArgumentNullException("edmVal");
                }
                // getEditableDoubleMult
                MethodInfo mjGetEDM = edmVal.GetGetMethod();
                if (mjGetEDM != null)
                {
                    getEditableDoubleMult = DynamicMethodFactory.CreateFunc<object, double>(mjGetEDM);
                }
                // setEditableDoubleMult
                MethodInfo mjSetEDM = edmVal.GetSetMethod();
                if (mjSetEDM != null)
                {
                    setEditableDoubleMult = DynamicMethodFactory.CreateDynFunc<object, double, object>(mjSetEDM);
                }

                //--- ModuleAscentAutoPilot
                FieldInfo turnRoll_t = mjModuleAscentAP_t.GetField("turnRoll");
                if (turnRoll_t == null)
                {
                    return;
                }
                GetAPTurnRoll = DynamicMethodFactory.CreateGetField<object, object>(turnRoll_t);
                FieldInfo verticalRoll_t = mjModuleAscentAP_t.GetField("verticalRoll");
                if (verticalRoll_t == null)
                {
                    return;
                }
                GetAPVerticalRoll = DynamicMethodFactory.CreateGetField<object, object>(verticalRoll_t);
                FieldInfo desiredOrbitAltitude_t = mjModuleAscentAP_t.GetField("desiredOrbitAltitude");
                if (desiredOrbitAltitude_t == null)
                {
                    return;
                }
                GetLaunchAltitude = DynamicMethodFactory.CreateGetField<object, object>(desiredOrbitAltitude_t);
                FieldInfo forceRoll_t = mjModuleAscentAP_t.GetField("forceRoll");
                if (forceRoll_t == null)
                {
                    return;
                }
                GetAPForceRoll = DynamicMethodFactory.CreateGetField<object, bool>(forceRoll_t);
                SetForceRoll = DynamicMethodFactory.CreateSetField<object, bool>(forceRoll_t);
                FieldInfo desiredOrbitInclination_t = mjModuleAscentAP_t.GetField("desiredInclination");
                if (desiredOrbitInclination_t == null)
                {
                    return;
                }
                GetLaunchInclination = DynamicMethodFactory.CreateGetField<object, double>(desiredOrbitInclination_t);
                SetLaunchInclination = DynamicMethodFactory.CreateSetField<object, double>(desiredOrbitInclination_t);

                //--- ModuleAscentGuidance
                FieldInfo desiredOrbitInclinationAG_t = mjModuleAscentGuid_t.GetField("desiredInclination");
                if (desiredOrbitInclinationAG_t == null)
                {
                    return;
                }
                GetInclinationAG = DynamicMethodFactory.CreateGetField<object, object>(desiredOrbitInclinationAG_t);

                //--- ModuleLandingAutopilot
                MethodInfo mjLandAtPositionTarget = mjLandingAutopilot_t.GetMethod("LandAtPositionTarget", BindingFlags.Instance | BindingFlags.Public);
                if (mjLandAtPositionTarget == null)
                {
                    throw new NotImplementedException("mjLandAtPositionTarget");
                }
                LandAtPositionTarget = DynamicMethodFactory.CreateDynFunc<object, object, object>(mjLandAtPositionTarget);
                MethodInfo mjLandUntargeted = mjLandingAutopilot_t.GetMethod("LandUntargeted", BindingFlags.Instance | BindingFlags.Public);
                if (mjLandUntargeted == null)
                {
                    throw new NotImplementedException("mjLandUntargeted");
                }
                LandUntargeted = DynamicMethodFactory.CreateDynFunc<object, object, object>(mjLandUntargeted);
                MethodInfo mjStopLanding = mjLandingAutopilot_t.GetMethod("StopLanding", BindingFlags.Instance | BindingFlags.Public);
                if (mjStopLanding == null)
                {
                    throw new NotImplementedException("mjStopLanding");
                }
                StopLanding = DynamicMethodFactory.CreateFunc<object, object>(mjStopLanding);

                //--- ModuleLandingPredictions
                Type mjModuleLandingPredictions_t = Utility.GetExportedType("MechJeb2", "MuMech.MechJebModuleLandingPredictions");
                if (mjModuleLandingPredictions_t == null)
                {
                    throw new NotImplementedException("mjModuleLandingPredictions_t");
                }
                MethodInfo mjPredictionsGetResult = mjModuleLandingPredictions_t.GetMethod("GetResult", BindingFlags.Instance | BindingFlags.Public);
                if (mjPredictionsGetResult == null)
                {
                    throw new NotImplementedException("mjPredictionsGetResult");
                }
                GetPredictionsResult = DynamicMethodFactory.CreateFunc<object, object>(mjPredictionsGetResult);

                //--- ModuleNodeExecutor
                MethodInfo mjExecuteOneNode = mjNodeExecutor_t.GetMethod("ExecuteOneNode", BindingFlags.Instance | BindingFlags.Public);
                if (mjExecuteOneNode == null)
                {
                    throw new NotImplementedException("mjExecuteOneNode");
                }
                ExecuteOneNode = DynamicMethodFactory.CreateDynFunc<object, object, object>(mjExecuteOneNode);
                MethodInfo mjAbortNode = mjNodeExecutor_t.GetMethod("Abort", BindingFlags.Instance | BindingFlags.Public);
                if (mjAbortNode == null)
                {
                    throw new NotImplementedException("mjAbortNode");
                }
                AbortNode = DynamicMethodFactory.CreateFunc<object, object>(mjAbortNode);

                //--- ModuleSmartASS
                saTarget_t = mjModuleSmartass_t.GetField("target", BindingFlags.Instance | BindingFlags.Public);
                if (saTarget_t == null)
                {
                    return;
                }
                FieldInfo modeTexts_t = mjModuleSmartass_t.GetField("ModeTexts", BindingFlags.Static | BindingFlags.Public);
                modeNames = (string[])modeTexts_t.GetValue(null);
                MethodInfo mjSmartassEngage = mjModuleSmartass_t.GetMethod("Engage", BindingFlags.Instance | BindingFlags.Public);
                if (mjSmartassEngage == null)
                {
                    throw new NotImplementedException("mjSmartassEngage");
                }
                Engage = DynamicMethodFactory.CreateDynFunc<object, bool, object>(mjSmartassEngage);

                //--- ModuleTargetController
                PropertyInfo mjPositionTargetExists = mjModuleTargetController_t.GetProperty("PositionTargetExists", BindingFlags.Instance | BindingFlags.Public);
                MethodInfo mjGetPositionTargetExists = null;
                if (mjPositionTargetExists != null)
                {
                    mjGetPositionTargetExists = mjPositionTargetExists.GetGetMethod();
                }
                if (mjGetPositionTargetExists == null)
                {
                    throw new NotImplementedException("mjGetPositionTargetExists");
                }
                PositionTargetExists = DynamicMethodFactory.CreateFunc<object, bool>(mjGetPositionTargetExists);

                //--- OrbitalManeuverCalculator
                MethodInfo deltaVToChangePeriapsis = mjOrbitalManeuverCalculator_t.GetMethod("DeltaVToChangePeriapsis", BindingFlags.Static | BindingFlags.Public);
                if (deltaVToChangePeriapsis == null)
                {
                    throw new ArgumentNullException("deltaVToChangePeriapsis");
                }
                DeltaVToChangePeriapsis = DynamicMethodFactory.CreateFunc<Orbit, double, double, Vector3d>(deltaVToChangePeriapsis);

                MethodInfo deltaVToChangeApoapsis = mjOrbitalManeuverCalculator_t.GetMethod("DeltaVToChangeApoapsis", BindingFlags.Static | BindingFlags.Public);
                if (deltaVToChangeApoapsis == null)
                {
                    throw new ArgumentNullException("deltaVToChangeApoapsis");
                }
                DeltaVToChangeApoapsis = DynamicMethodFactory.CreateFunc<Orbit, double, double, Vector3d>(deltaVToChangeApoapsis);

                MethodInfo deltaVToChangeCircularize = mjOrbitalManeuverCalculator_t.GetMethod("DeltaVToCircularize", BindingFlags.Static | BindingFlags.Public);
                if (deltaVToChangeCircularize == null)
                {
                    throw new ArgumentNullException("deltaVToChangeCircularize");
                }
                DeltaVToCircularize = DynamicMethodFactory.CreateDynFunc<Orbit, double, Vector3d>(deltaVToChangeCircularize);

                MethodInfo deltaVAndTimeForHohmannTransfer = mjOrbitalManeuverCalculator_t.GetMethod("DeltaVAndTimeForHohmannTransfer", BindingFlags.Static | BindingFlags.Public);
                if (deltaVAndTimeForHohmannTransfer == null)
                {
                    throw new NotImplementedException("deltaVAndTimeForHohmannTransfer");
                }
                DeltaVAndTimeForHohmannTransfer = DynamicMethodFactory.CreateFunc<object>(deltaVAndTimeForHohmannTransfer);

                MethodInfo deltaVToMatchVelocities = mjOrbitalManeuverCalculator_t.GetMethod("DeltaVToMatchVelocities", BindingFlags.Static | BindingFlags.Public);
                if (deltaVToMatchVelocities == null)
                {
                    throw new NotImplementedException("deltaVToMatchVelocities");
                }
                DeltaVToMatchVelocities = DynamicMethodFactory.CreateFunc<Orbit, double, Orbit, Vector3d>(deltaVToMatchVelocities);

                MethodInfo deltaVAndTimeForInterplanetaryTransferEjection = mjOrbitalManeuverCalculator_t.GetMethod("DeltaVAndTimeForInterplanetaryTransferEjection", BindingFlags.Static | BindingFlags.Public);
                if (deltaVAndTimeForInterplanetaryTransferEjection == null)
                {
                    throw new NotImplementedException("deltaVAndTimeForInterplanetaryTransferEjection");
                }
                DeltaVAndTimeForInterplanetaryTransferEjection = DynamicMethodFactory.CreateFunc<object>(deltaVAndTimeForInterplanetaryTransferEjection);

                //--- ReentrySimulation.Result
                Type mjReentrySim_t = Utility.GetExportedType("MechJeb2", "MuMech.ReentrySimulation");
                if (mjReentrySim_t == null)
                {
                    throw new NotImplementedException("mjReentrySim_t");
                }
                Type mjReentryResult_t = mjReentrySim_t.GetNestedType("Result");
                if (mjReentryResult_t == null)
                {
                    throw new NotImplementedException("mjReentryResult_t");
                }
                ReentryOutcome = mjReentryResult_t.GetField("outcome", BindingFlags.Instance | BindingFlags.Public);
                if (ReentryOutcome == null)
                {
                    throw new NotImplementedException("ReentryOutcome");
                }
                ReentryEndPosition = mjReentryResult_t.GetField("endPosition", BindingFlags.Instance | BindingFlags.Public);
                if (ReentryEndPosition == null)
                {
                    throw new NotImplementedException("ReentryEndPosition");
                }
                ReentryTime = mjReentryResult_t.GetField("endUT", BindingFlags.Instance | BindingFlags.Public);
                if (ReentryTime == null)
                {
                    throw new NotImplementedException("ReentryTime");
                }

                //--- StageStats
                MethodInfo mjRequestUpdate = mjModuleStageStats_t.GetMethod("RequestUpdate", BindingFlags.Instance | BindingFlags.Public);
                if (mjRequestUpdate == null)
                {
                    throw new NotImplementedException("mjRequestUpdate");
                }
                RequestUpdate = DynamicMethodFactory.CreateFunc<object, object, bool, object>(mjRequestUpdate);
                VacStats = mjModuleStageStats_t.GetField("vacStats", BindingFlags.Instance | BindingFlags.Public);
                if (VacStats == null)
                {
                    throw new NotImplementedException("VacStats");
                }

                //--- FuelFlowSimulation.Stats
                StatsStageDv = mjFuelFlowSimulationStats_t.GetField("deltaV", BindingFlags.Instance | BindingFlags.Public);
                if (StatsStageDv == null)
                {
                    throw new NotImplementedException("mjStageDv");
                }
                //Utility.LogMessage(StatsStageDv, "mjStageDv type is {0}", StatsStageDv.FieldType.Name);
                GetStageDv = DynamicMethodFactory.CreateGetField<object, object>(StatsStageDv);

                // Updated MechJeb (post 2.5.1) switched from using KER back to
                // its internal FuelFlowSimulation.  This sim uses an array of
                // structs, which entails a couple of extra hoops to jump through
                // when reading via reflection.
                AtmoStats = mjModuleStageStats_t.GetField("atmoStats", BindingFlags.Instance | BindingFlags.Public);
                if (AtmoStats == null)
                {
                    throw new NotImplementedException("AtmoStats");
                }

                PropertyInfo mjStageStatsLength = VacStats.FieldType.GetProperty("Length");
                if (mjStageStatsLength == null)
                {
                    throw new NotImplementedException("mjStageStatsLength");
                }
                MethodInfo mjStageStatsGetLength = mjStageStatsLength.GetGetMethod();
                if (mjStageStatsGetLength == null)
                {
                    throw new NotImplementedException("mjStageStatsGetLength");
                }
                GetStatsLength = DynamicMethodFactory.CreateFunc<object, int>(mjStageStatsGetLength);
                MethodInfo mjStageStatsGetIndex = VacStats.FieldType.GetMethod("Get");
                if (mjStageStatsGetIndex == null)
                {
                    throw new NotImplementedException("mjStageStatsGetIndex");
                }
                GetStatsIndex = DynamicMethodFactory.CreateDynFunc<object, int, object>(mjStageStatsGetIndex);

                //--- UserPool
                MethodInfo mjAddUser = mjUserPool_t.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);
                if (mjAddUser == null)
                {
                    throw new NotImplementedException("mjAddUser");
                }
                AddUser = DynamicMethodFactory.CreateDynFunc<object, object, object>(mjAddUser);
                MethodInfo mjRemoveUser = mjUserPool_t.GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public);
                if (mjRemoveUser == null)
                {
                    throw new NotImplementedException("mjRemoveUser");
                }
                RemoveUser = DynamicMethodFactory.CreateDynFunc<object, object, object>(mjRemoveUser);

                //--- VesselExtensions
                MethodInfo GetMasterMechJeb_t = mjVesselExtensions_t.GetMethod("GetMasterMechJeb", BindingFlags.Static | BindingFlags.Public);
                if (GetMasterMechJeb_t == null)
                {
                    return;
                }
                GetMasterMechJeb = DynamicMethodFactory.CreateFunc<Vessel, object>(GetMasterMechJeb_t);
                if (GetMasterMechJeb == null)
                {
                    return;
                }
                MethodInfo mjPlaceManeuverNode = mjVesselExtensions_t.GetMethod("PlaceManeuverNode", BindingFlags.Static | BindingFlags.Public);
                if (mjPlaceManeuverNode == null)
                {
                    throw new NotImplementedException("mjPlaceManeuverNode");
                }
                PlaceManeuverNode = DynamicMethodFactory.CreateFunc<Vessel, Orbit, Vector3d, double, object>(mjPlaceManeuverNode);

                //--- AbsoluteVector
                Type mjAbsoluteVector_t = Utility.GetExportedType("MechJeb2", "MuMech.AbsoluteVector");
                if (mjAbsoluteVector_t == null)
                {
                    throw new NotImplementedException("mjAbsoluteVector_t");
                }
                AbsoluteVectorLat = mjAbsoluteVector_t.GetField("latitude", BindingFlags.Instance | BindingFlags.Public);
                if (AbsoluteVectorLat == null)
                {
                    throw new NotImplementedException("AbsoluteVectorLat");
                }
                AbsoluteVectorLon = mjAbsoluteVector_t.GetField("longitude", BindingFlags.Instance | BindingFlags.Public);
                if (AbsoluteVectorLon == null)
                {
                    throw new NotImplementedException("AbsoluteVectorLon");
                }

                mjFound = true;
            }
            catch (Exception e)
            {
                Utility.LogStaticError("MJ static ctor exception {0}", e);
            }
        }
        #endregion
    }
}
