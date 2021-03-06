using UnityEngine;
using System;
using System.Collections;
using jp.nyatla.nyartoolkit.cs.markersystem;
using jp.nyatla.nyartoolkit.cs.core;
using NyARUnityUtils;
using System.IO;
using System.Threading;

public class ARCameraWithPSEyeBehaviour : MonoBehaviour
{
	// NyARToolKit
	private NyARUnityMarkerSystem markerSystem_;
	private NyARUnityPSEye psEye_;
	private int markerId_;
	public string MarkerName = "MarkerHiro"; 
	public GameObject MarkerObject  = null;
	private bool isDetect_  = false;
	
	// PS Eye
	public int PsEyeId   = 0;
	public int FrameRate = 60;
	
	// Camera Object
	public GameObject Background  = null;
	public Camera ARCamera  = null;
	
	// Background
	public int Layer       = 30;
	public int HiddenLayer = 31;
	
	// Somooth animation parameter
	private static float INELASTIC = 0.5f;
	public GameObject FilterPositionObject  = null;
	
	// Thread for updating marker system 
	private Thread thread_;
	private Mutex  mutex_;
	
	void Awake()
	{
		// Check PS Eye number
		try {
			int psEyeCount = PSEyeTexture.CLEyeGetCameraCount();
			switch (psEyeCount) {
				case 0:
					Debug.LogError("No PS Eye found");
					return;
				case 1:
					Debug.LogError("Only one PS Eye found");
					return;
			}
		} catch (Exception e) {
			Debug.LogError("Oops..., Failed at loading CLEyeMulticam.dll. Please relaunch Unity!");
			Debug.LogError(e.ToString());
			return;
		}
		
		// Make PS Eye texture
		var PsEyeTexture = new PSEyeTexture(PsEyeId, FrameRate, true);
		psEye_ = new NyARUnityPSEye(PsEyeTexture);
		
		// Marker system
		var config     = new NyARMarkerSystemConfig(PsEyeTexture.Width, PsEyeTexture.Height);
		markerSystem_  = new NyARUnityMarkerSystem(config);
		
		// Load marker from texture
		var markerTexture = (Texture2D)(Resources.Load(MarkerName, typeof(Texture2D)));
		markerId_ = markerSystem_.addARMarker(markerTexture, 16, 25, 80);
		
		// Marker layer
		MarkerObject.layer = Layer;
		for (int i = 0; i < MarkerObject.transform.GetChildCount(); ++i) {
			MarkerObject.transform.GetChild(i).gameObject.layer = Layer;
		}

		// Set camera background 
		Background.renderer.material.mainTexture = PsEyeTexture.Texture;
		Background.layer = Layer;
		ARCamera.cullingMask &= ~(1 << HiddenLayer);
		markerSystem_.setARBackgroundTransform(Background.transform);
		markerSystem_.setARCameraProjection(ARCamera);
	}
	
	void Start()
	{
		if (psEye_ == null) return; 
		psEye_.start();

		// Start thread
		mutex_  = new Mutex(true, "hoge");
		thread_ = new Thread(ThreadWorker);
		thread_.Start();
	}
	
	void Update()
	{
		if (psEye_ == null) return; 
		
		mutex_.ReleaseMutex();
		mutex_.WaitOne();
		
		// Update PS Eye texture
		psEye_.update();
		
		// Call event handlers
		if (markerSystem_.isExistMarker(markerId_)) {
			onFindMaker();
		} else {
			if (isDetect_) {
				onLostMarker();
			}
		}
		
		// Reset status
		if (Input.GetKey(KeyCode.Space)) {
			Debug.Log("Space");
			var miku = MarkerObject.transform.FindChild("Miku") as Transform; 
			miku.GetComponent<MikuAppearMotion>().Reset();
		}
	}
	
	void OnApplicationQuit() {
		thread_.Abort();
	}

	private void ThreadWorker() {
		try {
			_ThreadWorker();
		} catch (Exception e) {
			if (!(e is ThreadAbortException)) {
				Debug.LogError("Unexpected Death: " + e.ToString());
			}
		}	
	}
	
	private void _ThreadWorker() {
		for (;;) {
			Thread.Sleep(0);
			
			// Update marker system
			markerSystem_.update(psEye_);
			
			mutex_.WaitOne();
			mutex_.ReleaseMutex();
		}	
	}
	
	void onFindMaker()
	{
		MarkerObject.SetActive(true);
		StartCoroutine("MikuAppears");
		
		markerSystem_.setMarkerTransform(markerId_, FilterPositionObject);
		Transform targetTransform = FilterPositionObject.transform;
		targetTransform.Rotate(new Vector3(0, 180, 180));
		if (isDetect_) {
			MarkerObject.transform.localPosition = 
				Vector3.Slerp(MarkerObject.transform.localPosition, 
							  targetTransform.localPosition, 
							  INELASTIC);
			MarkerObject.transform.localRotation = 
				Quaternion.Slerp(MarkerObject.transform.localRotation, 
								 targetTransform.localRotation,
								 INELASTIC);
			MarkerObject.transform.localScale = 
				Vector3.Slerp(MarkerObject.transform.localScale,
							  targetTransform.localScale,
							  INELASTIC);
		} else {
			MarkerObject.transform.localPosition = targetTransform.localPosition;
			MarkerObject.transform.localRotation = targetTransform.localRotation;
			MarkerObject.transform.localScale    = targetTransform.localScale;
		}
		isDetect_ = true;
	}
	
	void onLostMarker()
	{
		MarkerObject.SetActive(false);
		isDetect_ = false;
	}
	
	private IEnumerator MikuAppears()
	{
		yield return new WaitForSeconds(2.0f);
		var miku = MarkerObject.transform.FindChild("Miku") as Transform;
		miku.GetComponent<MikuAppearMotion>().Appear();
	}
}
