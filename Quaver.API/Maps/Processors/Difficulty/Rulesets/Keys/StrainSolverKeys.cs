using Quaver.API.Enums;
using Quaver.API.Helpers;
using Quaver.API.Maps;
using Quaver.API.Maps.Processors.Difficulty.Optimization;
using Quaver.API.Maps.Processors.Difficulty.Rulesets.Keys.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quaver.API.Maps.Processors.Difficulty.Rulesets.Keys
{
    /// <summary>
    ///     Will be used to solve Strain Rating.
    /// </summary>
    public class StrainSolverKeys : StrainSolver
    {
        //todo: remove this later. TEMP
        public int Roll { get; set; } = 0;
        public int SJack { get; set; } = 0;
        public int TJack { get; set; } = 0;
        public int Bracket { get; set; } = 0;

        /// <summary>
        ///     Overall difficulty of the map
        /// </summary>
        public override float OverallDifficulty { get; internal set; } = 0;

        //public StrainConstantsKeys StrainConstantsKeys = new StrainConstantsKeys();

        /// <summary>
        ///     Constants used for solving
        /// </summary>
        public StrainConstantsKeys StrainConstants { get; private set; }

        /// <summary>
        /// TODO: remove this later. TEMPORARY.
        /// </summary>
        public string DebugString { get; private set; } = "";

        /// <summary>
        ///     Average note density of the map
        /// </summary>
        public float AverageNoteDensity { get; private set; } = 0;

        /// <summary>
        ///     Hit objects in the map used for solving difficulty
        /// </summary>
        public List<StrainSolverData> StrainSolverData { get; private set; } = new List<StrainSolverData>();

        /// <summary>
        ///     Assumes that the assigned hand will be the one to press that key
        /// </summary>
        private Dictionary<int, Hand> LaneToHand4K { get; set; } = new Dictionary<int, Hand>()
        {
            { 1, Hand.Left },
            { 2, Hand.Left },
            { 3, Hand.Right },
            { 4, Hand.Right }
        };

        /// <summary>
        ///     Assumes that the assigned hand will be the one to press that key
        /// </summary>
        private Dictionary<int, Hand> LaneToHand7K { get; set; } = new Dictionary<int, Hand>()
        {
            { 1, Hand.Left },
            { 2, Hand.Left },
            { 3, Hand.Left },
            { 4, Hand.Ambiguous },
            { 5, Hand.Right },
            { 6, Hand.Right },
            { 7, Hand.Right }
        };

        /// <summary>
        ///     Assumes that the assigned finger will be the one to press that key.
        /// </summary>
        private Dictionary<int, FingerState> LaneToFinger4K { get; set; } = new Dictionary<int, FingerState>()
        {
            { 1, FingerState.Middle },
            { 2, FingerState.Index },
            { 3, FingerState.Index },
            { 4, FingerState.Middle }
        };

        /// <summary>
        ///     Assumes that the assigned finger will be the one to press that key.
        /// </summary>
        private Dictionary<int, FingerState> LaneToFinger7K { get; set; } = new Dictionary<int, FingerState>()
        {
            { 1, FingerState.Ring },
            { 2, FingerState.Middle },
            { 3, FingerState.Index },
            { 4, FingerState.Thumb },
            { 5, FingerState.Index },
            { 6, FingerState.Middle },
            { 7, FingerState.Ring }
        };

        /// <summary>
        ///     const
        /// </summary>
        /// <param name="qua"></param>
        public StrainSolverKeys(Qua map, StrainConstants constants, ModIdentifier mods = ModIdentifier.None) : base(map, constants, mods)
        {
            // Cast the current Strain Constants Property to the correct type.
            StrainConstants = (StrainConstantsKeys)constants;

            // Don't bother calculating map difficulty if there's less than 2 hit objects
            if (map.HitObjects.Count < 2)
                return;

            // Solve for difficulty
            CalculateDifficulty(mods);
        }

        /// <summary>
        ///     Calculate difficulty of a map with given rate
        /// </summary>
        /// <param name="rate"></param>
        public void CalculateDifficulty(ModIdentifier mods)
        {
            // If map does not exist, ignore calculation.
            if (Map == null) return;

            // Get song rate from selected mods
            var rate = ModHelper.GetRateFromMods(mods);

            // Compute for overall difficulty
            ComputeNoteDensityData(rate);
            ComputeBaseStrainStates(rate);
            ComputeForChords();
            ComputeForFingerActions();
            ComputeForActionPatterns(); // todo: not implemented yet
            ComputeForRollManipulation();
            ComputeForJackManipulation();
            CalculateOverallDifficulty();
        }

        /// <summary>
        ///     Compute and generate Note Density Data.
        /// </summary>
        /// <param name="qssData"></param>
        /// <param name="qua"></param>
        private void ComputeNoteDensityData(float rate)
        {
            //MapLength = Qua.Length;
            AverageNoteDensity = SECONDS_TO_MILLISECONDS * Map.HitObjects.Count / (Map.Length * rate);

            //todo: solve note density graph
            // put stuff here
        }

        /// <summary>
        ///     Get Note Data, and compute the base strain weights
        ///     The base strain weights are affected by LN layering
        /// </summary>
        /// <param name="qssData"></param>
        /// <param name="qua"></param>
        private void ComputeBaseStrainStates(float rate)
        {
            // Add hit objects from qua map to qssData
            for (var i = 0; i < Map.HitObjects.Count; i++)
            {
                var curHitOb = new StrainSolverHitObject(Map.HitObjects[i]);
                var curStrainData = new StrainSolverData(curHitOb, rate);

                // Assign Finger and Hand States
                switch (Map.Mode)
                {
                    case GameMode.Keys4:
                        curHitOb.FingerState = LaneToFinger4K[Map.HitObjects[i].Lane];
                        curStrainData.Hand = LaneToHand4K[Map.HitObjects[i].Lane];
                        break;
                    case GameMode.Keys7:
                        curHitOb.FingerState = LaneToFinger7K[Map.HitObjects[i].Lane];
                        curStrainData.Hand = LaneToHand7K[Map.HitObjects[i].Lane];
                        break;
                }

                // Add Strain Solver Data to list
                StrainSolverData.Add(curStrainData);
            }

            /*
            // Solve LN
            // todo: put this in its own method maybe?
            for (var i = 0; i < StrainSolverData.Count - 1; i++)
            {
                var curHitOb = StrainSolverData[i];
                for (var j = i + 1; j < StrainSolverData.Count; j++)
                {
                    // If the target hit object is way outside the current LN end, don't bother iterating through the rest.
                    var nextHitOb = StrainSolverData[j];
                    if (nextHitOb.StartTime > curHitOb.EndTime + StrainConstants.LnEndThresholdMs)
                        break;

                    // Check to see if the target hitobject is layered inside the current LN
                    if (nextHitOb.Hand == curHitOb.Hand && nextHitOb.StartTime >= curHitOb.StartTime + StrainConstants.ChordClumpToleranceMs)
                    {
                        // Target hitobject's LN ends after current hitobject's LN end.
                        if (nextHitOb.EndTime > curHitOb.EndTime)
                        {
                            foreach (var k in curHitOb.HitObjects)
                            {
                                k.LnLayerType = LnLayerType.OutsideRelease;
                                k.LnStrainMultiplier = 1.5f; //TEMP STRAIN MULTIPLIER. use constant later.
                            }
                        }

                        // Target hitobject's LN ends before current hitobject's LN end
                        else if (nextHitOb.EndTime > 0)
                        {
                            foreach (var k in curHitOb.HitObjects)
                            {
                                k.LnLayerType = LnLayerType.InsideRelease;
                                k.LnStrainMultiplier = 1.2f; //TEMP STRAIN MULTIPLIER. use constant later.
                            }
                        }

                        // Target hitobject is not an LN
                        else
                        {
                            foreach (var k in curHitOb.HitObjects)
                            {
                                k.LnLayerType = LnLayerType.InsideTap;
                                k.LnStrainMultiplier = 1.05f; //TEMP STRAIN MULTIPLIER. use constant later.
                            }
                        }
                    }
                }
            }*/
        }

        /// <summary>
        ///     Iterate through the HitObject list and merges the chords together into one data point
        /// </summary>
        private void ComputeForChords()
        {
            // Search through whole hit object list and find chords
            for (var i = 0; i < StrainSolverData.Count - 1; i++)
            {
                for (var j = i + 1; j < StrainSolverData.Count; j++)
                {
                    var msDiff = StrainSolverData[j].StartTime - StrainSolverData[i].StartTime;

                    if (msDiff > StrainConstants.ChordClumpToleranceMs)
                        break;

                    if (Math.Abs(msDiff) <= StrainConstants.ChordClumpToleranceMs)
                    {
                        if (StrainSolverData[i].Hand == StrainSolverData[j].Hand)
                        {
                            // There should really only be one hit object for 4k, but maybe more than for 7k
                            foreach (var k in StrainSolverData[j].HitObjects)
                                StrainSolverData[i].HitObjects.Add(k);

                            // Remove chorded object
                            StrainSolverData.RemoveAt(j);
                        }
                    }
                }
            }

            // Solve finger state of every object once chords have been found and applied.
            for (var i = 0; i < StrainSolverData.Count; i++)
            {
                StrainSolverData[i].SolveFingerState();
            }
        }

        /// <summary>
        ///     Scans every finger state, and determines its action (JACK/TRILL/TECH, ect).
        ///     Action-Strain multiplier is applied in computation.
        /// </summary>
        /// <param name="qssData"></param>
        private void ComputeForFingerActions()
        {
            // Solve for Finger Action
            for (var i = 0; i < StrainSolverData.Count - 1; i++)
            {
                var curHitOb = StrainSolverData[i];

                // Find the next Hit Object in the current Hit Object's Hand
                for (var j = i + 1; j < StrainSolverData.Count; j++)
                {
                    var nextHitOb = StrainSolverData[j];
                    if (curHitOb.Hand == nextHitOb.Hand && nextHitOb.StartTime > curHitOb.StartTime)
                    {
                        // Determined by if there's a minijack within 2 set of chords/single notes
                        var actionJackFound = ((int)nextHitOb.FingerState & (1 << (int)curHitOb.FingerState - 1)) != 0;

                        // Determined by if a chord is found in either finger state
                        var actionChordFound = curHitOb.HandChord || nextHitOb.HandChord;

                        // Determined by if both fingerstates are exactly the same
                        var actionSameState = curHitOb.FingerState == nextHitOb.FingerState;

                        // Determined by how long the current finger action is
                        var actionDuration = nextHitOb.StartTime - curHitOb.StartTime;

                        // Apply the "NextStrainSolverDataOnCurrentHand" value on the current hit object and also apply action duration.
                        curHitOb.NextStrainSolverDataOnCurrentHand = nextHitOb;
                        curHitOb.FingerActionDurationMs = actionDuration;

                        //todo: REMOVE. this is for debuggin.
                        //DebugString += (i + " | jack: " + actionJackFound + ", chord: " + actionChordFound + ", samestate: " + actionSameState + ", c-index: " + curHitOb.HandChordState + ", h-diff: " + actionDuration + "\n");

                        // Trill/Roll
                        if (!actionChordFound && !actionSameState)
                        {
                            curHitOb.FingerAction = FingerAction.Roll;
                            curHitOb.ActionStrainCoefficient = GetCoefficientValue(actionDuration,
                                StrainConstants.RollLowerBoundaryMs,
                                StrainConstants.RollUpperBoundaryMs,
                                StrainConstants.RollMaxStrainValue,
                                StrainConstants.RollCurveExponential);
                            Roll++;
                        }

                        // Simple Jack
                        else if (actionSameState)
                        {
                            curHitOb.FingerAction = FingerAction.SimpleJack;
                            curHitOb.ActionStrainCoefficient = GetCoefficientValue(actionDuration,
                                StrainConstants.SJackLowerBoundaryMs,
                                StrainConstants.SJackUpperBoundaryMs,
                                StrainConstants.SJackMaxStrainValue,
                                StrainConstants.SJackCurveExponential);
                            SJack++;
                        }

                        // Tech Jack
                        else if (actionJackFound)
                        {
                            curHitOb.FingerAction = FingerAction.TechnicalJack;
                            curHitOb.ActionStrainCoefficient = GetCoefficientValue(actionDuration,
                                StrainConstants.TJackLowerBoundaryMs,
                                StrainConstants.TJackUpperBoundaryMs,
                                StrainConstants.TJackMaxStrainValue,
                                StrainConstants.TJackCurveExponential);
                            TJack++;
                        }

                        // Bracket
                        else
                        {
                            curHitOb.FingerAction = FingerAction.Bracket;
                            curHitOb.ActionStrainCoefficient = GetCoefficientValue(actionDuration,
                                StrainConstants.BracketLowerBoundaryMs,
                                StrainConstants.BracketUpperBoundaryMs,
                                StrainConstants.BracketMaxStrainValue,
                                StrainConstants.BracketCurveExponential);
                            Bracket++;
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Scans every finger action and compute a pattern multiplier.
        ///     Pattern manipulation, and inflated patterns are factored into calculation.
        /// </summary>
        /// <param name="qssData"></param>
        private void ComputeForActionPatterns()
        {
            
        }

        /// <summary>
        ///     Scans for roll manipulation. "Roll Manipulation" is definced as notes in sequence "A -> B -> A" with one action at least twice as long as the other.
        /// </summary>
        private void ComputeForRollManipulation()
        {
            const int rollManipulationCheckSize = 10;
            var curManipulationFound = new bool[rollManipulationCheckSize];
            var prevManipulationFound = new bool[rollManipulationCheckSize];
            var totalManipulationFound = 0;

            foreach (var data in StrainSolverData)
            {
                // Shift the array of found manipulation by 1.
                Array.Copy(prevManipulationFound, 0, curManipulationFound, 1, rollManipulationCheckSize - 1);
                curManipulationFound[0] = false;

                // if the last index of the array is true, decrease count.
                if (prevManipulationFound[rollManipulationCheckSize - 1])
                    totalManipulationFound--;

                // Check to see if the current data point has two other following points
                if (data.NextStrainSolverDataOnCurrentHand != null && data.NextStrainSolverDataOnCurrentHand.NextStrainSolverDataOnCurrentHand != null)
                {
                    var middle = data.NextStrainSolverDataOnCurrentHand;
                    var last = data.NextStrainSolverDataOnCurrentHand.NextStrainSolverDataOnCurrentHand;

                    if (data.FingerAction == FingerAction.Roll && middle.FingerAction == FingerAction.Roll)
                    {
                        if (data.FingerState == last.FingerState)
                        {
                            if (data.FingerActionDurationMs > middle.FingerActionDurationMs)
                            {
                                // Count manipulation
                                curManipulationFound[0] = true;
                                totalManipulationFound++;

                                // Apply multiplier
                                // todo: catch possible arithmetic error (division by 0)
                                // todo: implement constants
                                var durationRatio = Math.Max(data.FingerActionDurationMs / middle.FingerActionDurationMs, middle.FingerActionDurationMs / data.FingerActionDurationMs);
                                var durationMultiplier = 1 / (1 + ((durationRatio - 1) / 3f));
                                var manipulationFoundRatio = 1 - (float)(Math.Pow(totalManipulationFound / rollManipulationCheckSize, 0.8f)) * 0.4f;
                                data.RollManipulationStrainMultiplier = durationMultiplier * manipulationFoundRatio;
                            }
                        }
                    }
                }

                // Set prev array of found manipulation to current one.
                Array.Copy(curManipulationFound, prevManipulationFound, rollManipulationCheckSize);
            }
        }

        /// <summary>
        ///     Scans for jack manipulation. "Jack Manipulation" is defined as a succession of simple jacks. ("A -> A -> A")
        /// </summary>
        private void ComputeForJackManipulation()
        {
            const int jackManipulationCheckSize = 10;
            var curManipulationFound = new bool[jackManipulationCheckSize];
            var prevManipulationFound = new bool[jackManipulationCheckSize];
            var totalManipulationFound = 0;

            foreach (var data in StrainSolverData)
            {
                // Shift the array of found manipulation by 1.
                Array.Copy(prevManipulationFound, 0, curManipulationFound, 1, jackManipulationCheckSize - 1);
                curManipulationFound[0] = false;

                // if the last index of the array is true, decrease count.
                if (prevManipulationFound[jackManipulationCheckSize - 1])
                    totalManipulationFound--;

                // Check to see if the current data point has a following data point
                if (data.NextStrainSolverDataOnCurrentHand != null )
                {
                    var next = data.NextStrainSolverDataOnCurrentHand;

                    if (data.FingerAction == FingerAction.SimpleJack && next.FingerAction == FingerAction.SimpleJack)
                    {
                        // Count manipulation
                        curManipulationFound[0] = true;
                        totalManipulationFound++;

                        // Apply multiplier
                        // todo: catch possible arithmetic error (division by 0)
                        // todo: implement constants
                        // note:    83.3ms = 180bpm 1/4 vibro
                        //          88.2ms = 170bpm 1/4 vibro
                        //          93.7ms = 160bpm 1/4 vibro

                        // 35f = 35ms tolerance before hitting vibro point (88.2ms, 170bpm vibro)
                        var durationValue = Math.Min(1, Math.Max(0, ((88.2f + 35f) - data.FingerActionDurationMs) / 35f));
                        var durationMultiplier = 1 - (durationValue * 0.6f);
                        var manipulationFoundRatio = 1 - (float)(Math.Pow(totalManipulationFound / jackManipulationCheckSize, 0.8f)) * 0.35f;
                        data.RollManipulationStrainMultiplier = durationMultiplier * manipulationFoundRatio;
                    }
                }

                // Set prev array of found manipulation to current one.
                Array.Copy(curManipulationFound, prevManipulationFound, jackManipulationCheckSize);
            }
        }

        /// <summary>
        ///     Scans for LN layering and applies a multiplier
        /// </summary>
        private void ComputeForLnMultiplier()
        {

        }

        /// <summary>
        ///     Calculates the general difficulty of a map
        /// </summary>
        /// <param name="qssData"></param>
        private void CalculateOverallDifficulty()
        {
            // Calculate the strain value for every data point
            foreach (var data in StrainSolverData)
            {
                data.CalculateStrainValue();
            }

            // Solve for difficulty (temporary)
            // Difficulty is determined by how long each action is and how difficult they are.
            //  - longer actions have more weight due to it taking up more of the maps' length.
            //  - generally shorter actions are harder, but a bunch of hard actions are obviously more difficulty than a single hard action

            // overall difficulty = sum of all actions:(difficulty * action length) / map length
            // todo: action length is currently manually calculated.
            // todo: maybe store action length in StrainSolverData because it already gets calculated earlier?


            // todo: make this look better
            switch (Map.Mode)
            {
                case Enums.GameMode.Keys4:
                    OverallDifficulty = CalculateDifficulty4K();
                    break;

                case Enums.GameMode.Keys7:
                    OverallDifficulty = CalculateDifficulty7K();
                    break;
            }

            // calculate stamina (temp solution)
            // it just uses the map's length.
            // 10 seconds = 0.9x multiplier
            // 100 seconds = 1.0x multiplier
            // 1000 seconds = 1.1x multiplier
            // 10000 seconds = 1.2x multiplier, ect.
            //OverallDifficulty *= (float)(0.5 + Math.Log10(Map.Length / rate) / 10);
        }

        /// <summary>
        ///     Calculate the general difficulty for a 4K map
        /// </summary>
        /// <param name="rate"></param>
        /// <returns></returns>
        private float CalculateDifficulty4K()
        {
            float calculatedDiff = 0;

            // left hand
            foreach (var data in StrainSolverData)
            {
                if (data.Hand == Hand.Left)
                    calculatedDiff += data.TotalStrainValue;
            }

            // right hand
            foreach (var data in StrainSolverData)
            {
                if (data.Hand == Hand.Right)
                    calculatedDiff += data.TotalStrainValue;
            }

            // Calculate overall 4k difficulty
            calculatedDiff /= StrainSolverData.Count;

            // Get Overall 4k difficulty
            return calculatedDiff;
        }

        /// <summary>
        ///     Calculate the general difficulty for a 7k map
        /// </summary>
        /// <param name="rate"></param>
        /// <returns></returns>
        private float CalculateDifficulty7K()
        {
            //todo: Implement Ambiguious Hand in calculation
            float calculatedDiff = 0;

            // left hand
            foreach (var data in StrainSolverData)
            {
                if (data.Hand == Hand.Left)
                    calculatedDiff += data.TotalStrainValue;
            }

            // right hand
            foreach (var data in StrainSolverData)
            {
                if (data.Hand == Hand.Right)
                    calculatedDiff += data.TotalStrainValue;
            }

            // Get overall 7k Difficulty
            calculatedDiff /= StrainSolverData.Count;

            // Get Overall 7k difficulty
            return calculatedDiff;
        }

        /// <summary>
        ///     Used to calculate Coefficient for Strain Difficulty
        /// </summary>
        private float GetCoefficientValue(float duration, float xMin, float xMax, float strainMax, float exp)
        {
            // todo: temp. Linear for now
            // todo: apply cosine curve
            const float lowestDifficulty = 1;

            // calculate ratio between min and max value
            var ratio = Math.Max(0, (duration - xMin) / (xMax - xMin));
                ratio = 1 - Math.Min(1, ratio);

            // compute for difficulty
            return lowestDifficulty + (strainMax - lowestDifficulty) * (float)Math.Pow(ratio, exp);
        }
    }
}
