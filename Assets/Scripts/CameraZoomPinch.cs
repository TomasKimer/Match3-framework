using UnityEngine;
using System.Collections;

/// <summary>
/// Pinch to zoom camera control.
/// Adapted from http://answers.unity3d.com/questions/63909/pinch-zoom-camera.html
/// </summary>
public class CameraZoomPinch : MonoBehaviour {

    public int speed = 4;
    public int minFov = 30;
    public int maxFov = 120;
    public float minPinchSpeed = 0.1f;
    public float varianceInDistances = 2.0f;

    private float touchDelta;
    private Vector2 prevDist;
    private Vector2 curDist;
    private float speedTouch1;
    private float speedTouch2;


    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    void Update() {
        if (Input.touchCount == 2) {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved) {

                curDist = touch1.position - touch2.position;
                prevDist = (touch1.position - touch1.deltaPosition) - (touch2.position - touch2.deltaPosition);

                touchDelta = curDist.magnitude - prevDist.magnitude;

                speedTouch1 = touch1.deltaPosition.magnitude / touch1.deltaTime;
                speedTouch2 = touch2.deltaPosition.magnitude / touch2.deltaTime;

                if ((touchDelta + varianceInDistances <= 0) && (speedTouch1 > minPinchSpeed) && (speedTouch2 > minPinchSpeed)) {
                    camera.fieldOfView = Mathf.Clamp(gameObject.camera.fieldOfView + speed, minFov, maxFov);
                }
                else if ((touchDelta - varianceInDistances > 0) && (speedTouch1 > minPinchSpeed) && (speedTouch2 > minPinchSpeed)) {
                    camera.fieldOfView = Mathf.Clamp(gameObject.camera.fieldOfView - speed, minFov, maxFov);
                }
            }
        }
        else {
            if (Input.GetAxis("Mouse ScrollWheel") < 0) {
                camera.fieldOfView = Mathf.Clamp(gameObject.camera.fieldOfView + speed, minFov, maxFov);
            }
            else if (Input.GetAxis("Mouse ScrollWheel") > 0) {
                camera.fieldOfView = Mathf.Clamp(gameObject.camera.fieldOfView - speed, minFov, maxFov);
            }
        }
    }
}
