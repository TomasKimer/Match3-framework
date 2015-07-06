using UnityEngine;
using System.Collections;

/// <summary>
/// PingPong scale animation.
/// </summary>
public class ScalePingPong : MonoBehaviour {

    /// <summary>
    /// Minimum scale.
    /// </summary>
    public Vector3 minScale = new Vector3(1.05f, 1.05f, 1.05f);

    /// <summary>
    /// Maximum scale.
    /// </summary>
    public Vector3 maxScale = new Vector3(1.2f, 1.2f, 1.2f);

    /// <summary>
    /// Animation speed.
    /// </summary>
    public float speed = 2.0f;

    /// <summary>
    /// Use this for initialization.
    /// </summary>
    void Start() {
    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    void Update() {
        transform.localScale = Vector3.Lerp(minScale, maxScale, Mathf.PingPong(Time.time * speed, 1.0f));
    }
}
