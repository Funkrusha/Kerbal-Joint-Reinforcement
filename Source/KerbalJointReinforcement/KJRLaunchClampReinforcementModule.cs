﻿/*
Kerbal Joint Reinforcement /L
Copyright 2015, Michael Ferrara, aka Ferram4
Copyright 2018, LisiasT

Developers: Michael Ferrara (aka Ferram4), Meiru, LisiasT

    This file is part of Kerbal Joint Reinforcement.

    Kerbal Joint Reinforcement is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Kerbal Joint Reinforcement is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Kerbal Joint Reinforcement.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KerbalJointReinforcement
{
    //This class adds an extra joint between a launch clamp and the part it is connected to for stiffness
    public class KJRLaunchClampReinforcementModule : PartModule
    {
        private List<ConfigurableJoint> joints = new List<ConfigurableJoint>();
        private List<Part> neighbours = new List<Part>();
        //private bool decoupled = false;

        public override void OnAwake()
        {
            base.OnAwake();
        }

        public void OnDestroy()
        {
            if (joints.Count > 0)
                GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
        }

        public void OnPartPack()
        {
            if (joints.Count > 0)
                GameEvents.onVesselWasModified.Remove(OnVesselWasModified);

            foreach (ConfigurableJoint j in joints)
                GameObject.Destroy(j);

            joints.Clear();
            neighbours.Clear();
        }

        public void OnPartUnpack()
        {
            if (part.parent == null)
                return;

            neighbours.Add(part.parent);

            StringBuilder debugString = null;
            if (KJRJointUtils.debug)
            {
                debugString = new StringBuilder();
                debugString.AppendLine("The following joints added by " + part.partInfo.title + " to increase stiffness:");
            }

            if (part.parent.Rigidbody != null)
                joints.Add(KJRJointUtils.BuildJoint(part, part.parent));

            if (KJRJointUtils.debug)
            {
                debugString.AppendLine(part.parent.partInfo.title + " connected to part " + part.partInfo.title);
                Debug.Log(debugString.ToString());
            }

            if (joints.Count > 0)
                GameEvents.onVesselWasModified.Add(OnVesselWasModified);
        }

        private void OnVesselWasModified(Vessel v)
        {
            if (part.vessel == v)
            {
                foreach (Part p in neighbours)
                {
                    if (p.vessel == part.vessel)
                        continue;

                    if (KJRJointUtils.debug)
                        Debug.Log("Decoupling part " + part.partInfo.title + "; destroying all extra joints");

                    BreakAllInvalidJoints();
                    return;
                }
            }
        }

        private void BreakAllInvalidJoints()
        {
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);

            foreach (ConfigurableJoint j in joints)
                GameObject.Destroy(j);

            joints.Clear();

            var vessels = new List<Vessel>();

            foreach (Part n in neighbours)
            {
                if (n.vessel == null || vessels.Contains(n.vessel))
                    continue;

                vessels.Add(n.vessel);

                foreach (Part p in n.vessel.Parts)
                {
                    if (p.Modules.Contains<LaunchClamp>())
                        continue;

                    ConfigurableJoint[] possibleConnections = p.GetComponents<ConfigurableJoint>();

                    if (possibleConnections == null)
                        continue;

                    foreach (ConfigurableJoint j in possibleConnections)
                    {
                        if (j.connectedBody == null)
                        {
                            GameObject.Destroy(j);
                            continue;
                        }
                        Part cp = j.connectedBody.GetComponent<Part>();
                        if (cp != null && cp.vessel != p.vessel)
                            GameObject.Destroy(j);
                    }
                }
            }
            neighbours.Clear();
        }
    }
}
