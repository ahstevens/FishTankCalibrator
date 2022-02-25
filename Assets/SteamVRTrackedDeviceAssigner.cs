using UnityEngine;
using Valve.VR;

public class SteamVRTrackedDeviceAssigner : MonoBehaviour
{
    public string preferredTrackedDeviceName = "{htc}vr_tracker_vive_1_0";

    public System.Collections.Generic.List<GameObject> gameObjectsToAssignTracker;

    // Start is called before the first frame update
    void Start()
    {
        //help user find device number for their tracker by printing device IDs
        int trackerID = -1;
        SteamVR_TrackedObject.EIndex trackerEIndex = (SteamVR_TrackedObject.EIndex)1;
        var system = OpenVR.System;
        if (system == null)
        {
            Debug.Log("ERROR 724781 in CalibrationManager, SteamVR not active?");
        }
        else
        {
            for (int i = 0; i < 9; i++)
            {

                var error = ETrackedPropertyError.TrackedProp_Success;
                uint capacity = system.GetStringTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_RenderModelName_String, null, 0, ref error);
                if (capacity <= 1)
                {
                    Debug.Log("Failed to get render model name for tracked object device #:" + i);
                    continue;
                }

                var buffer = new System.Text.StringBuilder((int)capacity);
                system.GetStringTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_RenderModelName_String, buffer, capacity, ref error);

                Debug.Log("SteamVR Device #" + i + " is: " + buffer.ToString());

                //if (buffer.ToString().Contains("{htc}vr_tracker_vive_1_0") || buffer.ToString().Contains("{htc}vr_tracker_vive_3_0"))
                if (buffer.ToString().Contains(preferredTrackedDeviceName))
                {
                    trackerID = i;
                    trackerEIndex = (SteamVR_TrackedObject.EIndex)i;
                }
            }
        }

        if (trackerID == -1)
        {
            Debug.Log("ERROR 3815: Preferred Tracked Device Not Found!");
        }
        else
        {
            Debug.Log("Preferred Tracked Device Found: Device #" + trackerID);

            foreach (var g in gameObjectsToAssignTracker)
            {
                var trackedDeviceComponent = g.GetComponent<SteamVR_TrackedObject>();

                if (!trackedDeviceComponent)
                {
                    Debug.Log("GameObject " + g.name + " does not have a SteamVR_TrackedObject Component! Adding new...");

                    trackedDeviceComponent = g.AddComponent<SteamVR_TrackedObject>();
                }

                trackedDeviceComponent.index = trackerEIndex;
            }
        }
    }
}
