using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ActorPlayerTank : ActorTank 
{
	public static ActorPlayerTank current { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		current = this;
	}

	protected virtual void Update()
	{
		moveInput = Vector3.zero;   // combined movement input axes mapped to 3d space 
		aimInput = Vector3.zero;    // aiming direction mapped to 3d space 
		var inputCamera = Camera.main;
		if (inputCamera != null)
		{
			// calculate input from the camera's perspective            
			// .movement input
			var camForward = inputCamera.transform.forward;
			camForward.y = 0;
			camForward.Normalize();

			var camRight = inputCamera.transform.right;
			camRight.y = 0;
			camRight.Normalize();

			moveInput = camForward * Input.GetAxisRaw("Vertical") + camRight * Input.GetAxisRaw("Horizontal");

			// .aiming input 
			var mouseRay = inputCamera.ScreenPointToRay(Input.mousePosition); 
			var groundPlane = new Plane(Vector3.up, Vector3.zero);
			float rayDistToPlane = 0f;
			if (groundPlane.Raycast(mouseRay, out rayDistToPlane))
			{
				aimInput = (mouseRay.GetPoint(rayDistToPlane)-transform.position).normalized;
			}
		}

		if (Input.GetButtonDown("Fire1"))
		{
			FireWeapon();
		}
	}
}
