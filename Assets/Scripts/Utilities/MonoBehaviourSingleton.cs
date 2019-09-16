using UnityEngine;

public class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviourSingleton<T>
{
    protected static T instance;

    public static T Instance
    {
        get
        {
            if (!instance)
                instance = FindObjectOfType<T>();
            if (!instance)
            {
                GameObject gameObject = new GameObject(typeof(T).Name);
                instance = gameObject.AddComponent<T>();
            }

            return instance;
        }
    }

    protected virtual void Awake()
    {
        SetUpSingleton();
    }

    protected virtual void SetUpSingleton()
    {
        if (Instance != this)
            Destroy(gameObject);
        else
            DontDestroyOnLoad(gameObject);
    }
}
