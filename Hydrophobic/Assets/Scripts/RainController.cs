using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainController : MonoBehaviour
{
  // timers
  [SerializeField] private float timeSinceLastDownPour;
  [SerializeField] private float timeInBetweenDownPourAndDrizzle;
  [SerializeField] private float timeForDrizzle;
  [SerializeField] private float timeForDownPour;

  [SerializeField] private float distanceAbovePlayer;

  // components
  [SerializeField] private Transform player;
  [SerializeField] private PlayerRespawn playerRespawn;

  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {
    
    // move the rain controller above the character 
    Vector3 position = player.position;
    position.y += distanceAbovePlayer;
    transform.position = position;

    // cast the ray (filter to only look at layer 8 and 6)
    RaycastHit2D hitData = Physics2D.Linecast(transform.position, player.position, 1 << 8 | 1 << 6);
    
    // check if the player is hit
    if (hitData.rigidbody != null)
    {
      if (hitData.rigidbody.gameObject.layer == 8) // player
      {
        Debug.Log("I see the player");
      }
      else
      {
        Debug.Log("I do not see the player");
      }
    } else {
        Debug.Log("I see nothing");
    }
  }
}
