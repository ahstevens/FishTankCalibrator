using System;
using System.Xml;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;

public class CalibrationManager : MonoBehaviour
{
    [DllImport("ScreenCalibrator.dll")]
    private static extern IntPtr CalcUsingCorners(float ULx, float ULy, float ULz, float URx, float URy, float URz, float LRx, float LRy, float LRz, float LLx, float LLy, float LLz);

    [DllImport("ScreenCalibrator.dll")]
    private static extern void freeCalibration(IntPtr instance);

    [DllImport("ScreenCalibrator.dll")]
    private static extern float getCornerValue(IntPtr instance, int index);

    [DllImport("ScreenCalibrator.dll")]
    private static extern float getPlaneEQValue(IntPtr instance, int index);

    public GameObject probePoint;

    public string calibrationFilename = "calibration.xml";

    public bool loadCalibration = false;

    private GameObject currentScreen;
    private GameObject lastCornerObject;

    private Vector3 tmpScreenUL;
    private Vector3 tmpScreenUR;
    private Vector3 tmpScreenLR;
    private Vector3 tmpScreenLL;

    GameObject cornerSpherePrefab;

    String outPath;

    enum SurfaceCorner
    {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft,
        NONE
    } 
    
    private SurfaceCorner corner;

    private List<FishTankSurface> surfaces;

    // Start is called before the first frame update
    void Start()
    {
        surfaces = new List<FishTankSurface>();

        outPath = Application.dataPath;

        corner = SurfaceCorner.NONE;

        if (probePoint == null)
        {
            probePoint = GameObject.Find("probePoint");
        }

        cornerSpherePrefab = Resources.Load("CornerSphere", typeof(GameObject)) as GameObject;
    }


    // Update is called once per frame
    void Update()
    {
        if (loadCalibration)
        {
            ReadCalibration();
            loadCalibration = false;
        }


        if (Input.GetKeyDown(KeyCode.C))
        {
            SetCorner();
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            RemoveLastScreen();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RemoveLastCorner();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveCalibration();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            SetCamera();
        }
    }//end Update()

    void SetCorner()
    {
        if (corner == SurfaceCorner.NONE)
        {
            currentScreen = new GameObject("Screen " + surfaces.Count.ToString());
            corner = SurfaceCorner.TopLeft;
        }

        Debug.Log("Corner set: " + probePoint.transform.position.x + ", " + probePoint.transform.position.y + ", " + probePoint.transform.position.z);

        switch (corner)
        {
            case SurfaceCorner.TopLeft:
                tmpScreenUL = probePoint.transform.position;
                lastCornerObject = Instantiate(cornerSpherePrefab, currentScreen.transform);
                lastCornerObject.name = "Screen #" + surfaces.Count + " Probed Top Left";
                lastCornerObject.transform.position = tmpScreenUL;
                corner = SurfaceCorner.TopRight;
                break;

            case SurfaceCorner.TopRight:
                tmpScreenUR = probePoint.transform.position;
                lastCornerObject = Instantiate(cornerSpherePrefab, currentScreen.transform);
                //tmpCorner2.name = "Set Corner Point UR";
                lastCornerObject.name = "Screen #" + surfaces.Count + " Probed Top Right";
                lastCornerObject.transform.position = tmpScreenUR;
                corner = SurfaceCorner.BottomRight;
                break;

            case SurfaceCorner.BottomRight:
                tmpScreenLR = probePoint.transform.position;
                lastCornerObject = Instantiate(cornerSpherePrefab, currentScreen.transform);
                //tmpCorner3.name = "Set Corner Point LR";
                lastCornerObject.name = "Screen #" + surfaces.Count + " Probed Bottom Right";
                lastCornerObject.transform.position = tmpScreenLR;
                corner = SurfaceCorner.BottomLeft;
                break;

            case SurfaceCorner.BottomLeft:
                tmpScreenLL = probePoint.transform.position;
                lastCornerObject = Instantiate(cornerSpherePrefab, currentScreen.transform);
                //tmpCorner4.name = "Set Corner Point LL";
                lastCornerObject.name = "Screen #" + surfaces.Count + " Probed Bottom Left";
                lastCornerObject.transform.position = tmpScreenLL;

                FishTankSurface rectifiedScreen = Rectify(new FishTankSurface(tmpScreenUL, tmpScreenUR, tmpScreenLR, tmpScreenLL));

                SpawnRectifiedCorners(rectifiedScreen);

                var newSurface = SpawnQuad("Screen Surface " + surfaces.Count.ToString(), rectifiedScreen);

                newSurface.transform.parent = currentScreen.transform;

                surfaces.Add(rectifiedScreen);

                corner = SurfaceCorner.NONE;
                break;

            default:
                break;
        }
    }

    FishTankSurface Rectify(FishTankSurface probedScreen)
    {
        Debug.Log("Calculating Rectified Screen Corners");

        IntPtr screenCalibrator = CalcUsingCorners(tmpScreenUL.x, tmpScreenUL.y, tmpScreenUL.z, tmpScreenUR.x, tmpScreenUR.y, tmpScreenUR.z, tmpScreenLR.x, tmpScreenLR.y, tmpScreenLR.z, tmpScreenLL.x, tmpScreenLL.y, tmpScreenLL.z);

        var rectifiedScreen = new FishTankSurface(
            new Vector3(getCornerValue(screenCalibrator, 0), getCornerValue(screenCalibrator, 1), getCornerValue(screenCalibrator, 2)),
            new Vector3(getCornerValue(screenCalibrator, 3), getCornerValue(screenCalibrator, 4), getCornerValue(screenCalibrator, 5)),
            new Vector3(getCornerValue(screenCalibrator, 6), getCornerValue(screenCalibrator, 7), getCornerValue(screenCalibrator, 8)),
            new Vector3(getCornerValue(screenCalibrator, 9), getCornerValue(screenCalibrator, 10), getCornerValue(screenCalibrator, 11))
        );

        freeCalibration(screenCalibrator);

        //Debug.Log("Probed Top Left " + probedScreen.topLeft);
        //Debug.Log("Probed Top Right " + probedScreen.topRight);
        //Debug.Log("Probed Bottom Right " + probedScreen.bottomRight);
        //Debug.Log("Probed Bottom Left " + probedScreen.bottomLeft);
        //Debug.Log("Rectified Top Left " + rectifiedScreen.topLeft);
        //Debug.Log("Rectified Top Right " + rectifiedScreen.topRight);
        //Debug.Log("Rectified Bottom Right " + rectifiedScreen.bottomRight);
        //Debug.Log("Rectified Bottom Left " + rectifiedScreen.bottomLeft);

        return rectifiedScreen;
    }

    void SpawnRectifiedCorners(FishTankSurface rectifiedScreen)
    {
        GameObject tmpCorner5 = Instantiate(cornerSpherePrefab, currentScreen.transform);
        GameObject tmpCorner6 = Instantiate(cornerSpherePrefab, currentScreen.transform);
        GameObject tmpCorner7 = Instantiate(cornerSpherePrefab, currentScreen.transform);
        GameObject tmpCorner8 = Instantiate(cornerSpherePrefab, currentScreen.transform);
        tmpCorner5.name = "Screen #" + surfaces.Count + " Rectified Top Left";
        tmpCorner6.name = "Screen #" + surfaces.Count + " Rectified Top Right";
        tmpCorner7.name = "Screen #" + surfaces.Count + " Rectified Bottom Right";
        tmpCorner8.name = "Screen #" + surfaces.Count + " Rectified Bottom Left";
        tmpCorner5.transform.position = rectifiedScreen.topLeft;
        tmpCorner6.transform.position = rectifiedScreen.topRight;
        tmpCorner7.transform.position = rectifiedScreen.bottomRight;
        tmpCorner8.transform.position = rectifiedScreen.bottomLeft;
        tmpCorner5.GetComponent<MeshRenderer>().material.color = Color.red;
        tmpCorner6.GetComponent<MeshRenderer>().material.color = Color.red;
        tmpCorner7.GetComponent<MeshRenderer>().material.color = Color.red;
        tmpCorner8.GetComponent<MeshRenderer>().material.color = Color.red;
    }

    void RemoveLastScreen()
    {
        if (corner == SurfaceCorner.NONE && surfaces.Count > 0)
        {
            surfaces.RemoveAt(surfaces.Count - 1);
            Destroy(currentScreen);
            currentScreen = GameObject.Find("Screen " + surfaces.Count);
        }
    }

    void RemoveLastCorner()
    {
        switch (corner)
        {
            case SurfaceCorner.TopLeft:
                break;

            case SurfaceCorner.TopRight:
                break;

            case SurfaceCorner.BottomRight:
                break;

            case SurfaceCorner.BottomLeft:
                break;

            default:
                break;
        }
    }

    void SetCamera()
    {
        var trackedDevice = GameObject.Find("Calibration Probe");

        if (trackedDevice != null)
            Camera.main.transform.SetPositionAndRotation(trackedDevice.transform.position, trackedDevice.transform.rotation);
    }

    GameObject SpawnQuad(String name, FishTankSurface surface)
    {
        GameObject newQuad = new GameObject();
        newQuad.name = name;
        
        MeshRenderer meshRenderer = newQuad.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));

        MeshFilter meshFilter = newQuad.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4];
        vertices[0] = surface.bottomLeft;
        vertices[1] = surface.bottomRight;
        vertices[2] = surface.topLeft;
        vertices[3] = surface.topRight;
        mesh.vertices = vertices;

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

        return newQuad;
    }//end spawnNewQuad()

    void ReadCalibration()
    {
        Debug.Log("Reading Calibration File " + calibrationFilename);
        XmlReader reader = XmlReader.Create(outPath + "//" + calibrationFilename);

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    Debug.Log($"Start Element {reader.Name}");
                    break;
                case XmlNodeType.Text:
                    Debug.Log($"Text Node: {reader.Value}");
                    break;
                case XmlNodeType.EndElement:
                    Debug.Log($"End Element {reader.Name}");
                    break;
                default:
                    Debug.Log($"Other node {reader.NodeType} with value {reader.Value}");
                    break;
            }
        }
    }

    void SaveCalibration()
    {
        var sts = new XmlWriterSettings()
        {
            Indent = true,
        };

        XmlWriter writer = XmlWriter.Create(outPath + "//" + calibrationFilename, sts);

        writer.WriteStartDocument();
        writer.WriteStartElement("FishTank");

        for (int i = 0; i < surfaces.Count; ++i)
        {
            writer.WriteStartElement("screen");


            writer.WriteStartElement("number");
            writer.WriteValue(i);
            writer.WriteEndElement(); //number


            writer.WriteStartElement("topleft");

            writer.WriteStartElement("x");
            writer.WriteValue(surfaces[i].topLeft.x);
            writer.WriteEndElement(); //x

            writer.WriteStartElement("y");
            writer.WriteValue(surfaces[i].topLeft.y);
            writer.WriteEndElement(); //y

            writer.WriteStartElement("z");
            writer.WriteValue(surfaces[i].topLeft.z);
            writer.WriteEndElement(); //z

            writer.WriteEndElement(); //topleft


            writer.WriteStartElement("topright");

            writer.WriteStartElement("x");
            writer.WriteValue(surfaces[i].topRight.x);
            writer.WriteEndElement(); //x

            writer.WriteStartElement("y");
            writer.WriteValue(surfaces[i].topRight.y);
            writer.WriteEndElement(); //y

            writer.WriteStartElement("z");
            writer.WriteValue(surfaces[i].topRight.z);
            writer.WriteEndElement(); //z

            writer.WriteEndElement(); //topRight


            writer.WriteStartElement("bottomright");

            writer.WriteStartElement("x");
            writer.WriteValue(surfaces[i].bottomRight.x);
            writer.WriteEndElement(); //x

            writer.WriteStartElement("y");
            writer.WriteValue(surfaces[i].bottomRight.y);
            writer.WriteEndElement(); //y

            writer.WriteStartElement("z");
            writer.WriteValue(surfaces[i].bottomRight.z);
            writer.WriteEndElement(); //z

            writer.WriteEndElement(); //bottomRight


            writer.WriteStartElement("bottomleft");

            writer.WriteStartElement("x");
            writer.WriteValue(surfaces[i].bottomLeft.x);
            writer.WriteEndElement(); //x

            writer.WriteStartElement("y");
            writer.WriteValue(surfaces[i].bottomLeft.y);
            writer.WriteEndElement(); //y

            writer.WriteStartElement("z");
            writer.WriteValue(surfaces[i].bottomLeft.z);
            writer.WriteEndElement(); //z

            writer.WriteEndElement(); //bottomLeft


            writer.WriteEndElement(); //screen
        }

        writer.WriteEndElement(); //fishtank
        writer.WriteEndDocument();
    }
}
