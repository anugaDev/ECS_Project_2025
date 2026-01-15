using Cinemachine;
using Client;
using ElementCommons;
using Types;
using Units;
using Unity.Entities;
using UnityEngine;

namespace PlayerCamera
{
    public class CameraController : MonoBehaviour
    { 
        [SerializeField] 
        private CinemachineVirtualCamera _cinemachineVirtualCamera;

        [Header("Move Settings")] 

        [SerializeField]
        private bool _drawBounds;

        [SerializeField] 
        private Bounds _cameraBounds;
        
        [SerializeField] 
        private float _camSpeed;
        
        [SerializeField] 
        private Vector2 _screenPercentageDetection;

        [Header("Zoom Settings")] 

        [SerializeField]
        private float _minZoomDistance;

        [SerializeField] 
        private float _maxZoomDistance;

        [SerializeField] 
        private float _zoomSpeed;

        [Header("Camera Start Positions")] 
        
        [SerializeField]
        private Vector3 _redTeamPosition = new(50f, 50f, 50f);

        [SerializeField] 
        private Vector3 _blueTeamPosition = new(-50f, 50f, -50f);

        [SerializeField] 
        private Vector3 _spectatorPosition = new(0f, 50f, 0f);

        private CinemachineFramingTransposer _transposer;

        private EntityManager _entityManager;

        private EntityQuery _teamControllerQuery;

        private EntityQuery _localChampQuery;

        private bool _cameraSet;

        private void Awake()
        {
            _transposer = _cinemachineVirtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        }

        private void Start()
        {
            if (World.DefaultGameObjectInjectionWorld == null) return;
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _teamControllerQuery = _entityManager.CreateEntityQuery(typeof(ClientTeamRequest));
            _localChampQuery = _entityManager.CreateEntityQuery(typeof(OwnerTagComponent));
            SetCameraToOwnerTeam();
        }

        private void SetCameraToOwnerTeam()
        {
            if (!_teamControllerQuery.TryGetSingleton<ClientTeamRequest>(out ClientTeamRequest requestedTeam))
            {
                return;
            }

            SetCameraTeamStartingPosition(requestedTeam);
        }

        private void SetCameraTeamStartingPosition(ClientTeamRequest requestedTeam)
        {
            TeamType team = requestedTeam.Value;
            Vector3 cameraPosition = GetCameraPosition(team);
            transform.position = cameraPosition;

            if (team != TeamType.AutoAssign)
            {
                _cameraSet = true;
            }
        }

        private Vector3 GetCameraPosition(TeamType team)
        {
            return team switch
            {
                TeamType.Blue => _blueTeamPosition,
                TeamType.Red => _redTeamPosition,
                _ => _spectatorPosition
            };
        }

        private void Update()
        {
            SetCameraForAutoAssignTeam();
            MoveCamera();
            ZoomCamera();
        }

        private void MoveCamera()
        {
            if (Input.GetKey(KeyCode.A))
            {
                transform.position += Vector3.left * (_camSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.D))
            {
                transform.position += Vector3.right * (_camSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.S))
            {
                transform.position += Vector3.back * (_camSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.W))
            {
                transform.position += Vector3.forward * (_camSpeed * Time.deltaTime);
            }

            if (!_cameraBounds.Contains(transform.position))
            {
                transform.position = _cameraBounds.ClosestPoint(transform.position);
            }
        }

        private void ZoomCamera()
        {
            if (!(Mathf.Abs(Input.mouseScrollDelta.y) > float.Epsilon))
            {
                return;
            }
            _transposer.m_CameraDistance -= Input.mouseScrollDelta.y * _zoomSpeed * Time.deltaTime;
            _transposer.m_CameraDistance =
                Mathf.Clamp(_transposer.m_CameraDistance, _minZoomDistance, _maxZoomDistance);
        }

        private void SetCameraForAutoAssignTeam()
        {
            if (_cameraSet || !_localChampQuery.TryGetSingletonEntity<OwnerTagComponent>(out Entity localUnit))
            {
                return;
            }

            TeamType team = _entityManager.GetComponentData<ElementTeamComponent>(localUnit).Team;
            Vector3 cameraPosition = GetCameraPosition(team);
            transform.position = cameraPosition;
            _cameraSet = true;
        }

        private void OnDrawGizmos()
        {
            if (!_drawBounds) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_cameraBounds.center, _cameraBounds.size);
        }
    }
}