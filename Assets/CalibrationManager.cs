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
    
    struct ProjectionSurface
    {
        public Vector3 topLeft;
        public Vector3 topRight;
        public Vector3 bottomRight;
        public Vector3 bottomLeft;
        public ProjectionSurface(Vector3 topLeft, Vector3 topRight, Vector3 bottomRight, Vector3 bottomLeft)
        {
            this.topLeft = topLeft;
            this.topRight = topRight;
            this.bottomRight = bottomRight;
            this.bottomLeft = bottomLeft;
        }
    }

    enum SurfaceCorner
    {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft,
        NONE
    } 
    
    private SurfaceCorner corner;

    private List<ProjectionSurface> surfaces;

    // Start is called before the first frame update
    void Start()
    {
        surfaces = new List<ProjectionSurface>();

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

                ProjectionSurface rectifiedScreen = Rectify(new ProjectionSurface(tmpScreenUL, tmpScreenUR, tmpScreenLR, tmpScreenLL));

                SpawnRectifiedCorners(rectifiedScreen);

                var newSurface = SpawnQuad("Screen Surface " + surfaces.Count.ToString(), rectifiedScreen);

                newSurface.transform.parent = currentScreen.transform;

                var fts = newSurface.AddComponent<FishTankSurface>();
                fts.screenNumber = surfaces.Count;
                fts.topLeft = rectifiedScreen.topLeft;
                fts.topRight = rectifiedScreen.topRight;
                fts.bottomRight = rectifiedScreen.bottomRight;
                fts.bottomLeft = rectifiedScreen.bottomLeft;

                surfaces.Add(rectifiedScreen);

                corner = SurfaceCorner.NONE;
                break;

            default:
                break;
        }
    }

    ProjectionSurface Rectify(ProjectionSurface probedScreen)
    {
        Debug.Log("Calculating Rectified Screen Corners");

        IntPtr screenCalibrator = CalcUsingCorners(tmpScreenUL.x, tmpScreenUL.y, tmpScreenUL.z, tmpScreenUR.x, tmpScreenUR.y, tmpScreenUR.z, tmpScreenLR.x, tmpScreenLR.y, tmpScreenLR.z, tmpScreenLL.x, tmpScreenLL.y, tmpScreenLL.z);

        var rectifiedScreen = new ProjectionSurface(
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

    void SpawnRectifiedCorners(ProjectionSurface rectifiedScreen)
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

    GameObject SpawnQuad(String name, ProjectionSurface surface)
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
        using XmlReader reader = XmlReader.Create(outPath + "//" + calibrationFilename);

        reader.MoveToContent();

        if (reader.NodeType == XmlNodeType.Element && reader.Name != "FishTank")
        {
            Debug.Log("File " + calibrationFilename + " does not appear to be a valid FishTank calibration file!");
            reader.Close();
            return;
        }        

        while (reader.ReadToFollowing("screen"))
        {
            float x, y, z;
            Vector3 topleft, topright, bottomright, bottomleft;

            
            int screenNum = int.Parse(reader.GetAttribute("number"));
            Debug.Log("Reading Screen " + screenNum);
            
            reader.ReadToDescendant("topleft"); // move to topLeft corner
            x = Single.Parse(reader.GetAttribute("x"));
            y = Single.Parse(reader.GetAttribute("y"));
            z = Single.Parse(reader.GetAttribute("z"));
            topleft = new Vector3(x, y, z);


            reader.ReadToNextSibling("topright"); // move to topLeft corner
            x = Single.Parse(reader.GetAttribute("x"));
            y = Single.Parse(reader.GetAttribute("y"));
            z = Single.Parse(reader.GetAttribute("z"));
            topright = new Vector3(x, y, z);


            reader.ReadToNextSibling("bottomright"); // move to topLeft corner
            x = Single.Parse(reader.GetAttribute("x"));
            y = Single.Parse(reader.GetAttribute("y"));
            z = Single.Parse(reader.GetAttribute("z"));
            bottomright = new Vector3(x, y, z);

            reader.ReadToNextSibling("bottomleft"); // move to topLeft corner
            x = Single.Parse(reader.GetAttribute("x"));
            y = Single.Parse(reader.GetAttribute("y"));
            z = Single.Parse(reader.GetAttribute("z"));
            bottomleft = new Vector3(x, y, z);

            GameObject tmp = new GameObject("Screen " + screenNum.ToString());
            var fts = tmp.AddComponent<FishTankSurface>();
            fts.screenNumber = screenNum;
            fts.topLeft = topleft;
            fts.topRight = topright;
            fts.bottomRight = bottomright;
            fts.bottomLeft = bottomleft;
        }

        reader.Close();
    }

    void SaveCalibration()
    {
        Debug.Log("Saving " + surfaces.Count + " surfaces to calibration file " + calibrationFilename);

        var sts = new XmlWriterSettings()
        {
            Indent = true,
        };

        using XmlWriter writer = XmlWriter.Create(outPath + "//" + calibrationFilename, sts);

        writer.WriteStartDocument();
        writer.WriteStartElement("FishTank");

        for (int i = 0; i < surfaces.Count; ++i)
        {
            writer.WriteStartElement("screen");

            writer.WriteAttributeString("number", i.ToString());

            writer.WriteStartElement("topleft");

            writer.WriteAttributeString("x", surfaces[i].topLeft.x.ToString("F5"));
            writer.WriteAttributeString("y", surfaces[i].topLeft.y.ToString("F5"));
            writer.WriteAttributeString("z", surfaces[i].topLeft.z.ToString("F5"));

            writer.WriteEndElement(); //topleft


            writer.WriteStartElement("topright");

            writer.WriteAttributeString("x", surfaces[i].topRight.x.ToString("F5"));
            writer.WriteAttributeString("y", surfaces[i].topRight.y.ToString("F5"));
            writer.WriteAttributeString("z", surfaces[i].topRight.z.ToString("F5"));

            writer.WriteEndElement(); //topRight


            writer.WriteStartElement("bottomright");

            writer.WriteAttributeString("x", surfaces[i].bottomRight.x.ToString("F5"));
            writer.WriteAttributeString("y", surfaces[i].bottomRight.y.ToString("F5"));
            writer.WriteAttributeString("z", surfaces[i].bottomRight.z.ToString("F5"));

            writer.WriteEndElement(); //bottomRight


            writer.WriteStartElement("bottomleft");

            writer.WriteAttributeString("x", surfaces[i].bottomLeft.x.ToString("F5"));
            writer.WriteAttributeString("y", surfaces[i].bottomLeft.y.ToString("F5"));
            writer.WriteAttributeString("z", surfaces[i].bottomLeft.z.ToString("F5"));

            writer.WriteEndElement(); //bottomLeft


            writer.WriteEndElement(); //screen
        }

        writer.WriteEndElement(); //fishtank
        writer.WriteEndDocument();
    }
}
