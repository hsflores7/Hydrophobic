using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    #region VARIABLES

    // variables to control how movement feels
    [Header("Movement Stats")]
    [SerializeField] private float horizontalMaxSpeed;
    [SerializeField] private float horizontalAcceleration;
    private float horizontalAccelAmount; //The actual force (multiplied with speedDiff) applied to the player.
	[SerializeField] private float horizontalDecceleration; //The speed at which our player decelerates from their current speed.
	private float horizontalDeccelAmount; //Actual force (multiplied with speedDiff) applied to the player.
    private Vector2 _moveInput;
    [SerializeField] private bool doConserveMomentum;


    [Header("Jump Stats")]
    [SerializeField] private float jumpHeight; //Height of the player's jump.
	[SerializeField] private float jumpTimeToApex; //Time between applying the jump force and reaching the desired jump height.
	private float jumpForce; //The actual force applied (upwards) to the player when they jump.
    [SerializeField] private float jumpInputBufferTime;
    [SerializeField] private float coyoteTime; // Grace period after falling off a platform, where you can still jump

    [SerializeField] private float jumpCutGravityMult; // Multiplier to increase gravity if the player releases thje jump button while still jumping
	[SerializeField] private float jumpHangGravityMult; // Reduces gravity while close to the apex (desired max height) of the jump
	[SerializeField] private float jumpHangTimeThreshold; // Speeds (close to 0) where the player will experience extra "jump hang".
    [SerializeField] private float jumpHangAccelerationMult; 
	[SerializeField] private float jumpHangMaxSpeedMult; 	


    [Header("Gravity Stats")]
    [SerializeField] private float fallGravityMult; //Multiplier to the player's gravityScale when falling.
	[SerializeField] private float maxFallSpeed; //Maximum fall speed (terminal velocity) of the player when falling.
	[SerializeField] private float fastFallGravityMult; //Larger multiplier to the player's gravityScale when they press down
	[SerializeField] private float maxFastFallSpeed; //Maximum fall speed(terminal velocity) of the player when performing a faster fall.
    private float gravityStrength; // Downwards force (gravity) needed for the desired jump
    private float gravityScale; //Strength of the player's gravity as a multiplier of gravity
    [SerializeField] private float respawnFloatTime;
    [SerializeField] private float respawnFallGravityMult;
    [SerializeField] private float maxRespawnFallspeed;


    // booleans needed to control movement
    private bool IsFacingRight;
	 private bool IsGrounded;
     private bool _isJumpCut;
     private bool _isJumpFalling;

    // timers needed for movement 
     private float LastOnGroundTime;
     private float LastPressedJumpTime;
     [SerializeField] private float LastRespawnedTime;

    [Header("Movement Layers")]
    [SerializeField] private LayerMask _groundLayer;

    [Header("Checks")] 
	[SerializeField] private Transform _groundCheckPoint;
	//Size of groundCheck depends on the size of your character
	[SerializeField] private Vector2 _groundCheckSize;

    // components needed to control movement
    private Rigidbody2D rb2d;
    private Animator animator;

    #endregion


    #region VARIABLE SETUP
    // Unity Callback, called when the inspector updates
    private void OnValidate() 
    {
        // Calculate are run acceleration & deceleration forces using formula: amount = ((1 / Time.fixedDeltaTime) * acceleration) / runMaxSpeed
		horizontalAccelAmount = (50 * horizontalAcceleration) / horizontalMaxSpeed;
		horizontalDeccelAmount = (50 * horizontalDecceleration) / horizontalMaxSpeed;

        // Calculate gravity strength using the formula (gravity = 2 * jumpHeight / timeToJumpApex^2) 
		gravityStrength = -(2 * jumpHeight) / (jumpTimeToApex * jumpTimeToApex);
        //Calculate the rigidbody's gravity scale (ie: gravity strength relative to unity's gravity value, see project settings/Physics2D)
		gravityScale = gravityStrength / Physics2D.gravity.y;

        // Calculate jumpForce using the formula (initialJumpVelocity = gravity * timeToJumpApex)
        jumpForce = Mathf.Abs(gravityStrength) * jumpTimeToApex; 
    }

    // Start is called before the first frame update
    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        IsFacingRight = false;
    }

    #endregion

    // Update is called once per frame
    void Update()
    {
        #region TIMERS
        LastOnGroundTime -= Time.deltaTime;
		LastPressedJumpTime -= Time.deltaTime;
        LastRespawnedTime -= Time.deltaTime;
		#endregion

        #region INPUT HANDLER
        
        if(!IsGrounded) {
		    _moveInput.x = Input.GetAxisRaw("Horizontal");
        }
        
		_moveInput.y = Input.GetAxisRaw("Vertical");

		if (_moveInput.x != 0)
			CheckDirectionToFace(_moveInput.x > 0);

		if(Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.J))
        {
			OnPressJumpInput();
        }

		if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.C) || Input.GetKeyUp(KeyCode.J))
		{
			OnReleaseJumpInput();
		}
		#endregion

        #region COLLISION CHECKS
		//Ground Check
		if (Physics2D.OverlapBox(_groundCheckPoint.position, _groundCheckSize, 0, _groundLayer)) // checks if set box overlaps with ground
		{
            IsGrounded = true;
            #region KILL ALL MOVEMENT
            _moveInput.x  = 0;
            //Calculate the direction we want to move in and our desired velocity
		    float targetSpeed = 0;
		
		    //Calculate difference between current velocity and desired velocity
		    float speedDif = targetSpeed - rb2d.velocity.x;

		    //Convert this to a vector and apply to rigidbody
		    rb2d.AddForce(speedDif * Vector2.right, ForceMode2D.Force);
            #endregion

			LastOnGroundTime = coyoteTime; // sets the lastGrounded to coyoteTime
            animator.SetBool("isGrounded", true);
            animator.SetBool("isFalling", false);
            animator.SetBool("isFloating", false);
            animator.SetBool("isJumping", false);
        } else {
            IsGrounded = false;
        }	

		
		#endregion

        #region JUMP CHECKS

        if (!IsGrounded && rb2d.velocity.y < 0)
		{
			_isJumpFalling = true;
		}

		if (LastOnGroundTime > 0 && IsGrounded)
        {
			_isJumpCut = false;
            _isJumpFalling = false;
		}

		//Jump
		if (CanJump() && LastPressedJumpTime > 0)
		{
			IsGrounded = false;
			_isJumpCut = false;
			_isJumpFalling = false;
			Jump();
		}

        #endregion
        
        #region GRAVITY
        // Lower gravity if we've just respawned 
        if (LastRespawnedTime > 0) {
            // lower gravity if holding down
			SetGravityScale(gravityScale * respawnFallGravityMult);
			// Caps maximum fall speed, so can't fall to fast
			rb2d.velocity = new Vector2(rb2d.velocity.x, Mathf.Max(rb2d.velocity.y, -maxRespawnFallspeed));
        }
		//Higher gravity if we've released the jump input or are falling
		else if (rb2d.velocity.y < 0 && _moveInput.y < 0)
		{
			//Much higher gravity if holding down
			SetGravityScale(gravityScale * fastFallGravityMult);
			//Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
			rb2d.velocity = new Vector2(rb2d.velocity.x, Mathf.Max(rb2d.velocity.y, -maxFastFallSpeed));
		}
		else if (_isJumpCut)
		{
			//Higher gravity if jump button released
			SetGravityScale(gravityScale * jumpCutGravityMult);
			rb2d.velocity = new Vector2(rb2d.velocity.x, Mathf.Max(rb2d.velocity.y, -maxFallSpeed));
		}
		else if ((!IsGrounded || _isJumpFalling) && Mathf.Abs(rb2d.velocity.y) < jumpHangTimeThreshold)
		{
			SetGravityScale(gravityScale * jumpHangGravityMult);
		}
		else if (rb2d.velocity.y < 0)
		{
			//Higher gravity if falling
			SetGravityScale(gravityScale * fallGravityMult);
			//Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
			rb2d.velocity = new Vector2(rb2d.velocity.x, Mathf.Max(rb2d.velocity.y, -maxFallSpeed));
		}
		else
		{
			//Default gravity if standing on a platform or moving upwards
			SetGravityScale(gravityScale);
		}
		#endregion

        #region ANIMATIONS
        animator.SetFloat("vertialVelocity", rb2d.velocity.y);
        animator.SetBool("isFloating", rb2d.velocity.y < 0 && rb2d.velocity.y >= -maxFallSpeed);
        animator.SetBool("isFalling", rb2d.velocity.y < -maxFallSpeed);
        if (!IsGrounded) {
            animator.SetBool("isGrounded", false);
        }
        if (rb2d.velocity.y > 0) {
            animator.SetBool("isJumping", true);
        }
        animator.SetFloat("lastRespawnTime", LastRespawnedTime);
        #endregion
    
    }

    public void updateRespawnTime() {
        LastRespawnedTime = respawnFloatTime;
        animator.SetFloat("lastRespawnTime", LastRespawnedTime);
    }

    private void FixedUpdate()
	{
		//Handle Horizontal Movement - only happens when jump
		if (CanMoveHorizontal()) {
			HorizontalMove(1);
        }
    }

    #region GRAVITY METHODS
    public void SetGravityScale(float scale)
	{
		rb2d.gravityScale = scale;
	}
    #endregion

    #region MOVE METHODS
    private void HorizontalMove(float lerpAmount) 
    {
        //Calculate the direction we want to move in and our desired velocity
		float targetSpeed = _moveInput.x * horizontalMaxSpeed;
		//We can reduce are control using Lerp() this smooths changes to are direction and speed
		targetSpeed = Mathf.Lerp(rb2d.velocity.x, targetSpeed, lerpAmount);

		#region Calculate AccelRate
		float accelRate;
		//Gets an acceleration value based on if we are accelerating (includes turning) 
		accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? horizontalAccelAmount : horizontalDeccelAmount;
        #endregion
		
		#region Add Bonus Jump Apex Acceleration
		//Increase are acceleration and maxSpeed when at the apex of their jump, makes the jump feel a bit more bouncy, responsive and natural
		if ((!IsGrounded || _isJumpFalling) && Mathf.Abs(rb2d.velocity.y) < jumpHangTimeThreshold)
		{
			accelRate *= jumpHangAccelerationMult;
			targetSpeed *= jumpHangMaxSpeedMult;
		}
		#endregion

		#region Conserve Momentum
		// We won't slow the player down if they are moving in their desired direction but at a greater speed than their maxSpeed
		if(doConserveMomentum && Mathf.Abs(rb2d.velocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(rb2d.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && LastOnGroundTime < 0)
		{
			//Prevent any deceleration from happening, or in other words conserve are current momentum
			//You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
			accelRate = 0; 
		}
		#endregion

		//Calculate difference between current velocity and desired velocity
		float speedDif = targetSpeed - rb2d.velocity.x;
		//Calculate force along x-axis to apply to thr player
		float movement = speedDif * accelRate;

		//Convert this to a vector and apply to rigidbody
		rb2d.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }

    private void Jump()
    {
        //Ensures we can't call Jump multiple times from one press
		LastPressedJumpTime = 0;
		LastOnGroundTime = 0;

		#region Perform Jump
		// We increase the force applied if we are falling
		// This means we'll always feel like we jump the same amount  
        float currJumpFource = jumpForce;
		if (rb2d.velocity.y < 0) {
			currJumpFource -= rb2d.velocity.y;
        }

		rb2d.AddForce(Vector2.up * currJumpFource, ForceMode2D.Impulse);
        
		#endregion

        animator.SetBool("isGrounded", false);
        animator.SetBool("isJumping", true);
    }

    private void Turn()
	{
		//stores scale and flips the player along the x axis, 
		Vector3 scale = transform.localScale; 
		scale.x *= -1;
		transform.localScale = scale;

		IsFacingRight = !IsFacingRight;
	}

    #endregion

    #region INPUT CALLBACKS
	//Methods which whandle input detected in Update()
    public void OnPressJumpInput()
	{
		LastPressedJumpTime = jumpInputBufferTime;
	}

	public void OnReleaseJumpInput()
	{
		if (CanJumpCut())
			_isJumpCut = true;
	}
    #endregion

    #region CHECK METHODS
    public void CheckDirectionToFace(bool isMovingRight)
	{
		if (isMovingRight != IsFacingRight)
			Turn();
	}

    private bool CanJump()
    {
		return LastOnGroundTime > 0 && IsGrounded;
    }

    private bool CanMoveHorizontal() 
    {
        return !IsGrounded;
    }

    private bool CanJumpCut()
    {
		return !IsGrounded && rb2d.velocity.y > 0;
    }

    #endregion
}
