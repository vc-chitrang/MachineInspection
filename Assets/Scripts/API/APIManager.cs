using System;
using System.Collections.Generic;

public class API {
    public static string APIDevelopmentBaseURL = "http://192.168.1.237:8000/";
    public static string APIProductionBaseURL = "http://192.168.1.237:8000/";

    public static string Predict = APIBaseURL + "predict";

    public static string APIBaseURL {
        get {
            switch (Server.Development) {
                case Server.Live:
                return APIProductionBaseURL;
                case Server.Development:
                return APIDevelopmentBaseURL;
            }

            return APIProductionBaseURL;
        }
    }

    public enum Server {
        Live,
        Development,
    }
}

[Serializable]
public class BoundingBox {
    public double x1;
    public double y1;
    public double x2;
    public double y2;
}

[Serializable]
public class Detection {
    public string label;
    public double confidence;
    public bool isSuccess;
    public BoundingBox bounding_box;
    public string description;
}
[Serializable]
public class ImageRecognitionResponse {
    public string filename;
    public List<Detection> detections;
}