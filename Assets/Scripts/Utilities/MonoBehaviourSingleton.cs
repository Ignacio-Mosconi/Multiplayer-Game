using UnityEngine;

public class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviourSingleton<T>
{
    static MonoBehaviourSingleton<T> instance;

    public static T Instance
    {
        get
        {
            if (!instance)
                instance = FindObjectOfType<MonoBehaviourSingleton<T>>();
            if (!instance)
            {
                GameObject gameObject = new GameObject("MonoBehaviour Singleton");
                instance = gameObject.AddComponent<MonoBehaviourSingleton<T>>();
            }

            return (T)instance;
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
    }
}
