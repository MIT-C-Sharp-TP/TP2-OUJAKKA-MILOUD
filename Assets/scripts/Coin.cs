using System;
using UnityEngine;

// A coin that the car picks up. Its collider has "Is Trigger" ticked.
// The coin spins like a flipped coin, floats up and down a little,
// and is worth a number of points that you set in the Inspector.
public class Coin : MonoBehaviour
{
    // raised when any coin is picked up: it sends the points of that coin
    public static event Action<int> Collected;

    [Header("Value")]
    [SerializeField] private int points = 1;               // what this coin is worth

    [Header("Spin")]
    [SerializeField] private float rotationSpeed = 120f;   // degrees per second

    [Header("Float (set Bob Height to 0 to turn it off)")]
    [SerializeField] private float bobHeight = 0.15f;      // metres, up and down
    [SerializeField] private float bobSpeed = 2f;          // how fast it floats

    [Header("Other")]
    [SerializeField] private GameObject collectEffect;     // optional

    private float startY;

    // lets the counter read the value without being able to change it
    public int Points => points;

    void Start()
    {
        startY = transform.position.y;
    }

    void Update()
    {
        // Space.World: the coin turns around the vertical axis, like a flipped coin
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);

        // float up and down
        Vector3 position = transform.position;
        position.y = startY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = position;
    }

    void OnTriggerEnter(Collider other)
    {
        // only the car collects coins
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        Collected?.Invoke(points);
        Destroy(gameObject);
    }
}
