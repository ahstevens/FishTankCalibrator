using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

public class CalibrationManager : MonoBehaviour
{
    public string preferredTrackerName;
    public GameObject controller;
    public GameObject calibrationProbe;

    [DllImport("ScreenCalibrator.dll")]
    private static extern IntPtr CalcUsingCorners(float ULx, float ULy, float ULz, float URx, float URy, float URz, float LRx, float LRy, float LRz, float LLx, float LLy, float LLz);

    [DllImport("ScreenCalibrator.dll")]
    private static extern void freeCalibration(IntPtr instance);

    [DllImport("ScreenCalibrator.dll")]
    private static extern float getCornerValue(IntPtr instance, int index);

    [DllImport("ScreenCalibrator.dll")]
    private static extern float getPlaneEQValue(IntPtr instance, int index);

    private GameObject probePoint;

    private Vector3 tmpScreenUL;
    private Vector3 tmpScreenUR;
    private Vector3 tmpScreenLR;
    private Vector3 tmpScreenLL;
    private int onCorner;
    private int screenCounter;

    GameObject cornerSpherePrefab;

    String outPath;

    // Start is called before the first frame update
    void Start()
    {
        screenCounter = 0;
        onCorner = 0;
        //string timeAtStart = System.DateTime.Now.ToString();
        //Debug.Log("Time at Start:" + timeAtStart);
        //outPath = "C:\\ARStudyLogs\\" + timeAtStart.Replace('/', '-').Replace(' ', '-').Replace(':', '-') + "\\";
        outPath = Application.dataPath;
        Debug.Log("Out Path:" + outPath);


        probePoint = GameObject.Find("probePoint");
        cornerSpherePrefab = Resources.Load("CornerSphere", typeof(GameObject)) as GameObject;

        //help user find device number for their tracker by printing device IDs
        int trackerID = -1;
        SteamVR_TrackedObject.EIndex trackerEIndex = (SteamVR_TrackedObject.EIndex)1;
        var system = OpenVR.System;
        if (system == null)
        {
            Debug.Log("ERROR 724781 in CalibrationManager, SteamVR not active?");
        }
        else for (int i = 0; i < 9; i++)
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
                if (buffer.ToString().Contains(preferredTrackerName))
                {
                    trackerID = i;
                    trackerEIndex = (SteamVR_TrackedObject.EIndex)i;
                }
            }
        if (trackerID == -1)
        {
            Debug.Log("ERROR 3815: Preferred Tracker Device Not Found!");
        }
        else
        {
            Debug.Log("Preferred Tracker Device Found: Device #" + trackerID);
            calibrationProbe.GetComponent<SteamVR_TrackedObject>().index = trackerEIndex;
            controller.GetComponent<SteamVR_TrackedObject>().index = trackerEIndex;
            controller.GetComponent<SteamVR_RenderModel>().index = trackerEIndex;
        }

    }


    // Update is called once per frame
    void Update()
    {

        //if (Input.GetKeyDown(KeyCode.S))
        //{
        
        //    if (screenCounter == 0)
        //    {
                
        //    }


        //    onCorner = 0;
        //}
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Corner set: " + probePoint.transform.position.x + ", " + probePoint.transform.position.y + ", " + probePoint.transform.position.z);
            if (onCorner == 0)
            {
                tmpScreenUL = probePoint.transform.position;
                GameObject tmpCorner1 = Instantiate(cornerSpherePrefab);
                tmpCorner1.name = "Screen #" + screenCounter + " Set UL";
                tmpCorner1.transform.position = tmpScreenUL;
                onCorner = 1;
            }
            else if (onCorner == 1)
            {
                tmpScreenUR = probePoint.transform.position;
                GameObject tmpCorner2 = Instantiate(cornerSpherePrefab);
                //tmpCorner2.name = "Set Corner Point UR";
                tmpCorner2.name = "Screen #" + screenCounter + " Set UR";
                tmpCorner2.transform.position = tmpScreenUR;
                onCorner = 2;
            }
            else if (onCorner == 2)
            {
                tmpScreenLR = probePoint.transform.position;
                GameObject tmpCorner3 = Instantiate(cornerSpherePrefab);
                //tmpCorner3.name = "Set Corner Point LR";
                tmpCorner3.name = "Screen #" + screenCounter + " Set LR";
                tmpCorner3.transform.position = tmpScreenLR;
                onCorner = 3;
            }
            else if (onCorner == 3)
            {
                tmpScreenLL = probePoint.transform.position;
                GameObject tmpCorner4 = Instantiate(cornerSpherePrefab);
                //tmpCorner4.name = "Set Corner Point LL";
                tmpCorner4.name = "Screen #" + screenCounter + " Set LL";
                tmpCorner4.transform.position = tmpScreenLL;

                Debug.Log("Calculating Rectified Screen Corners");

                Debug.Log("odl UL " + tmpScreenUL);
                Debug.Log("old UR " + tmpScreenUR);
                Debug.Log("old LR " + tmpScreenLR);
                Debug.Log("old LL " + tmpScreenLL);
                IntPtr screenCalibrator = CalcUsingCorners(tmpScreenUL.x, tmpScreenUL.y, tmpScreenUL.z, tmpScreenUR.x, tmpScreenUR.y, tmpScreenUR.z, tmpScreenLR.x, tmpScreenLR.y, tmpScreenLR.z, tmpScreenLL.x, tmpScreenLL.y, tmpScreenLL.z);
                Debug.Log("calibrator address: " + screenCalibrator); // => Show the pointer address.
                Vector3 newUL = new Vector3(getCornerValue(screenCalibrator, 0), getCornerValue(screenCalibrator, 1), getCornerValue(screenCalibrator, 2));
                Vector3 newUR = new Vector3(getCornerValue(screenCalibrator, 3), getCornerValue(screenCalibrator, 4), getCornerValue(screenCalibrator, 5));
                Vector3 newLR = new Vector3(getCornerValue(screenCalibrator, 6), getCornerValue(screenCalibrator, 7), getCornerValue(screenCalibrator, 8));
                Vector3 newLL = new Vector3(getCornerValue(screenCalibrator, 9), getCornerValue(screenCalibrator, 10), getCornerValue(screenCalibrator, 11));
                Debug.Log("new UL " + newUL);
                Debug.Log("new UR " + newUR);
                Debug.Log("new LR " + newLR);
                Debug.Log("new LL " + newLL);


                GameObject tmpCorner5 = Instantiate(cornerSpherePrefab);
                GameObject tmpCorner6 = Instantiate(cornerSpherePrefab);
                GameObject tmpCorner7 = Instantiate(cornerSpherePrefab);
                GameObject tmpCorner8 = Instantiate(cornerSpherePrefab);
                tmpCorner5.name = "Screen #" + screenCounter + " Fixed Corner UL";
                tmpCorner6.name = "Screen #" + screenCounter + " Fixed Corner UR";
                tmpCorner7.name = "Screen #" + screenCounter + " Fixed Corner LR";
                tmpCorner8.name = "Screen #" + screenCounter + " Fixed Corner LL";
                tmpCorner5.transform.position = newUL;
                tmpCorner6.transform.position = newUR;
                tmpCorner7.transform.position = newLR;
                tmpCorner8.transform.position = newLL;
                tmpCorner5.GetComponent<MeshRenderer>().material.color = Color.red;
                tmpCorner6.GetComponent<MeshRenderer>().material.color = Color.red;
                tmpCorner7.GetComponent<MeshRenderer>().material.color = Color.red;
                tmpCorner8.GetComponent<MeshRenderer>().material.color = Color.red;

                spawnNewQuad("Screen " + screenCounter.ToString(), newUL, newUR, newLR, newLL);


                //WRITE EVERYTHING ABOUT THIS SCREEN TO FILE
                if (screenCounter == 0)
                {
                    WriteToFile("<CAVE>\n");
                }
                WriteToFile("  <Screen>\n");
                WriteToFile("    <Number>" + screenCounter + "</Number>\n");
                WriteToFile("    <UL>" + newUL.x + ", " + newUL.y + ", " + newUL.z + "</UL>\n");
                WriteToFile("    <UR>" + newUR.x + ", " + newUR.y + ", " + newUR.z + "</UR>\n");
                WriteToFile("    <LR>" + newLR.x + ", " + newLR.y + ", " + newLR.z + "</LR>\n");
                WriteToFile("    <LL>" + newLL.x + ", " + newLL.y + ", " + newLL.z + "</LL>\n");
                WriteToFile("  </Screen>\n");

                screenCounter++;
                onCorner = 0;
                freeCalibration(screenCalibrator);
                //Debug.Log("Corner set: " + probePoint.transform.position.x + ", " + probePoint.transform.position.y + ", " + probePoint.transform.position.z);


            }

        }

        if (Input.GetKeyDown(KeyCode.E))
        {

        }



    }//end Update()

    void OnDisable()
    {
        if (screenCounter > 0)
        {
            WriteToFile("</CAVE>\n");
        }
    }//end OnDisable()

    void spawnNewQuad(String name, Vector3 UL, Vector3 UR, Vector3 LR, Vector3 LL)
    {
        GameObject newQuad = new GameObject();
        newQuad.name = name;
        
        MeshRenderer meshRenderer = newQuad.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));

        MeshFilter meshFilter = newQuad.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4];
        vertices[0] = LL;
        vertices[1] = LR;
        vertices[2] = UL;
        vertices[3] = UR;
        mesh.vertices = vertices;

        //Debug.Log("Mesh vert 1 " + mesh.vertices[0]);
        //Debug.Log("Mesh vert 2 " + mesh.vertices[1]);
        //Debug.Log("Mesh vert 3 " + mesh.vertices[2]);
        //Debug.Log("Mesh vert 4 " + mesh.vertices[3]);

        int[] tris = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };
        mesh.triangles = tris;


        Vector3[] normals = new Vector3[4]
        {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
        };
        mesh.normals = normals;

        mesh.RecalculateNormals();

        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        mesh.uv = uv;

        meshFilter.mesh = mesh;
    }//end spawnNewQuad()

    void WriteToFile(string text)
    {
        bool retValue;
        try
        {
            if (!System.IO.Directory.Exists(outPath))
                System.IO.Directory.CreateDirectory(outPath);
            System.IO.File.AppendAllText(outPath + "//calibration.xml", text);
            retValue = true;
        }
        catch (System.Exception ex)
        {
            string ErrorMessages = "File Write Error # 2224\n" + ex.Message;
            retValue = false;
            Debug.LogError(ErrorMessages);
        }
    }//end writeToFile()


}
