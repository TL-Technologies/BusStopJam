using UnityEngine;
using Watermelon.BusStop;
using Watermelon.SkinStore;

namespace Watermelon
{
    public class GameController : MonoBehaviour
    {
        private static GameController gameController;

        [DrawReference]
        [SerializeField] GameData data;

        [SerializeField] UIController uiController;

        private ParticlesController particlesController;
        private CurrenciesController currenciesController;
        private LevelController levelController;
        private TutorialController tutorialController;
        private PUController powerUpsController;

        private static bool isGameActive;
        public static bool IsGameActive => isGameActive;

        public static event SimpleCallback OnLevelChangedEvent;
        private static LevelSave levelSave;

        public static GameData Data => gameController.data;

        private void Awake()
        {
            gameController = this;

            SaveController.Initialise(useAutoSave: false);
            levelSave = SaveController.GetSaveObject<LevelSave>("level");

            // Cache components
            CacheComponent(out particlesController);
            CacheComponent(out currenciesController);
            CacheComponent(out levelController);
            CacheComponent(out tutorialController);
            CacheComponent(out powerUpsController);
            
        }

        private void Start()
        {
            InitialiseGame();
        }

        public void InitialiseGame()
        {
            uiController.Initialise();

            particlesController.Initialise();
            currenciesController.Initialise();
            tutorialController.Initialise();
            powerUpsController.Initialise();
            
            uiController.InitialisePages();

            // Add raycast controller component
            RaycastController raycastController = gameObject.AddComponent<RaycastController>();
            raycastController.Initialise();

            SkinStoreController.Init();

            levelController.Initialise();

            // Display default page
            UIController.ShowPage<UIMainMenu>();

            LoadLevel(() => GameLoading.MarkAsReadyToHide());
        }

        private static void LoadLevel(System.Action OnComplete = null)
        {
            gameController.levelController.LoadLevel(() =>
            {
                OnComplete?.Invoke();
            });

            OnLevelChangedEvent?.Invoke();
        }

        public static void StartGame()
        {
            // On Level is loaded
            isGameActive = true;

            UIController.HidePage<UIMainMenu>();
            UIController.ShowPage<UIGame>();

            Tween.DelayedCall(2f, LivesManager.RemoveLife);
        }

        public static void LoseGame()
        {
            if (!isGameActive)
                return;

            isGameActive = false;

            RaycastController.Disable();

            UIController.HidePage<UIGame>();
            UIController.ShowPage<UIGameOver>();

            AudioController.PlaySound(AudioController.Sounds.failSound);

            levelSave.ReplayingLevelAgain = true;
        }

        public static void WinGame()
        {
            if (!isGameActive)
                return;

            isGameActive = false;

            RaycastController.Disable();

            levelSave.ReplayingLevelAgain = false;

            LevelData completedLevel = LevelController.LoadedStageData;

            UIController.HidePage<UIGame>();
            UIController.ShowPage<UIComplete>();

            AudioController.PlaySound(AudioController.Sounds.completeSound);
        }

        public static void LoadNextLevel()
        {
            if (isGameActive)
                return;

            UIController.ShowPage<UIMainMenu>();

            levelSave.ReplayingLevelAgain = false;
            gameController.levelController.AdjustLevelNumber();


            LoadLevel();
        }

        public static void ReplayLevel()
        {
            isGameActive = false;

            UIController.ShowPage<UIMainMenu>();

            levelSave.ReplayingLevelAgain = true;


            LoadLevel();
        }

        public static void RefreshLevelDev()
        {
            UIController.ShowPage<UIGame>();
            levelSave.ReplayingLevelAgain = true;

            LoadLevel();
        }

        private void OnApplicationQuit()
        {
            // to make sure we will load similar level next time game launched (in case we outside level bounds)
            levelSave.ReplayingLevelAgain = true;
        }

        #region Extensions
        public bool CacheComponent<T>(out T component) where T : Component
        {
            Component unboxedComponent = gameObject.GetComponent(typeof(T));

            if (unboxedComponent != null)
            {
                component = (T)unboxedComponent;

                return true;
            }

            Debug.LogError(string.Format("Scripts Holder doesn't have {0} script added to it", typeof(T)));

            component = null;

            return false;
        }
        #endregion
    }
}