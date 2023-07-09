using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
  [SerializeField] private Transform player;
  [SerializeField] private PlayerMovement playerMovement;
  [SerializeField] private Transform respawnPoint;

  [SerializeField] private Rigidbody2D rb2d;


  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {

  }

  private void OnTriggerEnter2D(Collider2D col)
  {
    if (col.gameObject.layer == 7)
    { // 7 = respawn layer
      StartCoroutine(doTheRespawn());
    }
  }

  IEnumerator doTheRespawn()
  {
    playerMovement.updateRespawnTime();

    // have a mini jump
    rb2d.AddForce(Vector2.up * 4, ForceMode2D.Impulse);

    // wait for a couple seconds
    yield return new WaitForSeconds(1f);

    // move back to respawn point
    player.transform.position = respawnPoint.transform.position;
    Physics.SyncTransforms();
  }
}
