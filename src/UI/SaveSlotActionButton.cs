using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public class SaveSlotActionButton : MenuButton, ISubmitHandler, IEventSystemHandler, IPointerClickHandler, ISelectHandler
{
	public Animator? selectIcon;

	public Action? onSubmit;

	public static readonly int _isSelectedProp = Animator.StringToHash("Is Selected");

	public override void OnMove(AxisEventData eventData)
	{
		if (TryNavigateSkippingDisabled(eventData, this))
		{
			// handled
		}
		else
		{
			base.OnMove(eventData);
		}
	}

	public void Navigate(AxisEventData eventData, Selectable sel)
	{
		if (sel.IsActive() && sel.IsInteractable())
		{
			eventData.selectedObject = sel.gameObject;
		}
		else
		{
			TryNavigateSkippingDisabled(eventData, sel);
		}
	}

	public new void OnSubmit(BaseEventData eventData)
	{
		if (base.interactable)
		{
			base.OnSubmit(eventData);
			ForceDeselect();
			onSubmit?.Invoke();
		}
	}

	public new void OnPointerClick(PointerEventData eventData)
	{
		OnSubmit(eventData);
	}

	public new void OnSelect(BaseEventData eventData)
	{
		base.OnSelect(eventData);
		if (GetComponent<CanvasGroup>().interactable)
		{
			if ((bool)selectIcon)
			{
				selectIcon.SetBool(_isSelectedProp, value: true);
			}
		}
		else
		{
			StartCoroutine(SelectAfterFrame(base.navigation.selectOnUp.gameObject));
		}
	}

	protected override void OnDeselected(BaseEventData eventData)
	{
		if ((bool)selectIcon)
		{
			selectIcon.SetBool(_isSelectedProp, value: false);
		}
	}

	public IEnumerator SelectAfterFrame(GameObject obj)
	{
		yield return new WaitForEndOfFrame();
		EventSystem.current.SetSelectedGameObject(obj);
	}

	public static bool TryNavigateSkippingDisabled(AxisEventData eventData, Selectable start)
	{
		if (eventData.moveDir != MoveDirection.Right && eventData.moveDir != MoveDirection.Left)
		{
			return false;
		}

		var current = start;

		while (true)
		{
			var next = eventData.moveDir == MoveDirection.Right
				? current.FindSelectableOnRight()
				: current.FindSelectableOnLeft();

			if (next == null)
			{
				return false;
			}

			current = next;

			if (current.IsActive() && current.IsInteractable())
			{
				eventData.selectedObject = current.gameObject;
				return true;
			}
		}
	}
}
