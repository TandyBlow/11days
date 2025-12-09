// CameraController.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("相机组件")]
    private Camera mainCamera;
    [SerializeField] private LayerMask groundLayerMask;

    [Header("缩放设置")]
    [SerializeField] private float zoomSpeed = 0.01f;
    [SerializeField] private float minZoomSize = 5f;
    [SerializeField] private float maxZoomSize = 25f;

    [Header("平移设置")]
    [SerializeField] private float panSpeed = 0.015f;
    [SerializeField] private Vector2 panLimitMin = new Vector2(-20, -15);
    [SerializeField] private Vector2 panLimitMax = new Vector2(20, 15);

    [Header("点击/拖拽判定")]
    [SerializeField] private float tapThresholdDuration = 0.2f;
    [SerializeField] private float tapThresholdDistance = 10f; // 屏幕像素距离

    public Vector3 flashbackCameraPosition = new Vector3(0, 0, -10);
    public float flashbackCameraSize = 15f;
    private Vector3 originalCameraPosition;
    private float originalCameraSize;

    private Vector3 touchStartPosition;
    private float touchStartTime;

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        // 【改动】 允许在 Normal, Building 和 Finale 状态下操作相机
        GameState currentState = GameManager.Instance.CurrentState;
        if (currentState != GameState.Normal &&
            currentState != GameState.Building &&
            currentState != GameState.Finale) // <-- 新增此行
        {
            return;
        }

        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
#if UNITY_EDITOR
        else
        {
            HandleMouseInput();
        }
#endif
    }

    private void HandleTouchInput()
    {
        switch (Input.touchCount)
        {
            case 1:
                HandleSingleTouch();
                break;
            case 2:
                HandleDoubleTouch();
                break;
        }
    }

    private void HandleSingleTouch()
    {
        Touch touch = Input.GetTouch(0);
        if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;

        switch (touch.phase)
        {
            case TouchPhase.Began:
                touchStartTime = Time.time;
                touchStartPosition = touch.position;
                break;
            case TouchPhase.Moved:
                PanCamera(touch.deltaPosition);
                break;
            case TouchPhase.Ended:
                float touchDuration = Time.time - touchStartTime;
                float touchDistance = Vector2.Distance(touchStartPosition, touch.position);
                if (touchDuration < tapThresholdDuration && touchDistance < tapThresholdDistance)
                {
                    HandleTap(touch.position);
                }
                break;
        }
    }

    private void HandleDoubleTouch()
    {
        Touch touchZero = Input.GetTouch(0);
        Touch touchOne = Input.GetTouch(1);
        if (EventSystem.current.IsPointerOverGameObject(touchZero.fingerId) || EventSystem.current.IsPointerOverGameObject(touchOne.fingerId)) return;
        
        Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
        Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
        float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
        float currentMagnitude = (touchZero.position - touchOne.position).magnitude;
        float difference = currentMagnitude - prevMagnitude;
        ZoomCamera(difference * zoomSpeed);
    }
    
#if UNITY_EDITOR
    private void HandleMouseInput()
    {
        if (Input.mouseScrollDelta.y != 0)
        {
             if (EventSystem.current.IsPointerOverGameObject()) return;
            ZoomCamera(Input.mouseScrollDelta.y);
        }

        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            PanCamera(mouseDelta * -15f); 
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            HandleTap(Input.mousePosition);
        }
    }
#endif

    private void PanCamera(Vector2 delta)
    {
        Vector3 move = -new Vector3(delta.x, delta.y, 0) * panSpeed * (mainCamera.orthographicSize / maxZoomSize);
        transform.Translate(move, Space.Self);
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, panLimitMin.x, panLimitMax.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, panLimitMin.y, panLimitMax.y);
        transform.position = clampedPosition;
    }

    private void ZoomCamera(float increment)
    {
        mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize - increment, minZoomSize, maxZoomSize);
    }

    private void HandleTap(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayerMask))
        {
            if (GameManager.Instance.CurrentState == GameState.Building)
            {
                BuildManager.Instance.HandleTapOnGrid(hit.point);
            }
            // 【改动】在Finale状态下，点击不再触发建筑交互，但相机仍然可以移动
            else if (GameManager.Instance.CurrentState == GameState.Normal)
            {
                if (hit.collider.TryGetComponent<BuildingInstance>(out var buildingInstance))
                {
                    buildingInstance.HandleInteraction();
                }
            }
        }
    }

    public IEnumerator MoveToView(Vector3 targetPosition, float targetSize, float duration)
    {
        originalCameraPosition = transform.position;
        originalCameraSize = mainCamera.orthographicSize;
        float timer = 0f;
        Vector3 startPos = transform.position;
        float startSize = mainCamera.orthographicSize;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, timer / duration);
            transform.position = Vector3.Lerp(startPos, targetPosition, progress);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, progress);
            yield return null;
        }
        transform.position = targetPosition;
        mainCamera.orthographicSize = targetSize;
    }
    
    public IEnumerator RestoreOriginalView(float duration)
    {
        yield return StartCoroutine(MoveToView(originalCameraPosition, originalCameraSize, duration));
    }
}
