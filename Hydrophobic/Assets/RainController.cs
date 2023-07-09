using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainController : MonoBehaviour
{
  // timers
  [SerializeField] private float timeSinceLastDownPour;
  [SerializeField] private float timeInBetweenDownPourAndDrizzel;
  [SerializeField] private float timeForDrizzel;
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
    
    // move above the character 
    Vector3 position = player.position;
    position.y += distanceAbovePlayer;
    transform.position = position;

    /*

    // what way to point to hit player
    var rayDirection = player.position - transform.position;

    // cast the ray

    RaycastHit2D hitData = Physics2D.Raycast(transform.position, rayDirection, distanceAbovePlayer + 30f);
    if (hitData.collider != null)
    {
      if (hitData.transform.gameObject.layer == 8) // player
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
    */

    
    RaycastHit2D hitData = Physics2D.Linecast(transform.position, player.position);
    if (hitData.collider != null) 
    {
      Debug.Log("blocked");
    } else {
        Debug.Log("I See player");
    }
    

  }
}
