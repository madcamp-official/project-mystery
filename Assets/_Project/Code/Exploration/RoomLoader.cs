using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seat0A.Exploration
{
    public class RoomLoader : MonoBehaviour
    {
        public static RoomLoader Instance { get; private set; }

        public event Action<RoomDefinition> RoomLoaded;

        public RoomDefinition CurrentRoom { get; private set; }

        private Scene loadedScene;
        private bool isLoading;

        private void Awake()
        {
            Instance = this;
        }

        public void LoadRoom(RoomDefinition room)
        {
            if (room == null || isLoading || (CurrentRoom == room && loadedScene.IsValid()))
            {
                return;
            }

            StartCoroutine(LoadRoutine(room));
        }

        private IEnumerator LoadRoutine(RoomDefinition room)
        {
            isLoading = true;

            if (loadedScene.IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(loadedScene);
            }

            yield return SceneManager.LoadSceneAsync(room.SceneName, LoadSceneMode.Additive);
            loadedScene = SceneManager.GetSceneByName(room.SceneName);
            CurrentRoom = room;
            isLoading = false;
            RoomLoaded?.Invoke(room);
        }
    }
}
