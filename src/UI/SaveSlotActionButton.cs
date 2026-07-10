using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public class SaveSlotActionButton : MenuButton, ISubmitHandler, IEventSystemHandler, IPointerClickHandler, ISelectHandler
{
	public Animator? selectIcon;

	public Sprite? m_iconIdle;
	public Sprite? m_iconTransition;
	public Sprite? m_iconSelected;

	public Action? onSubmit;

	public static readonly int _isSelectedProp = Animator.StringToHash("Is Selected");

	public void Initialize(string assetName, Action onSubmit)
	{
		this.onSubmit = onSubmit;
		this.buttonType = MenuButton.MenuButtonType.Activate;

		var toRemove = this.GetComponent<ZeroAlphaOnStart>();
		if (toRemove != null)
		{
			UnityEngine.Object.Destroy(toRemove);
		}

		var assembly = Assembly.GetExecutingAssembly();
		m_iconIdle = LoadTexture(assembly, $"SaveFileManagerMod.assets.finished.{assetName}_01.png");
		m_iconTransition = LoadTexture(assembly, $"SaveFileManagerMod.assets.finished.{assetName}_02.png");
		m_iconSelected = LoadTexture(assembly, $"SaveFileManagerMod.assets.finished.{assetName}_03.png");

		var icon = transform.Find("Trash Icon")!;
		icon.name = $"{assetName} Icon";

		var image = icon.gameObject.GetComponent<UnityEngine.UI.Image>()!;
		image.sprite = m_iconIdle;
		image.overrideSprite = m_iconIdle;
	}

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

	public static Sprite? LoadTexture(Assembly assembly, string resourceName)
	{
		using (Stream stream = assembly.GetManifestResourceStream(resourceName))
		{
			if (stream == null)
			{
				SfmLogger.LogError($"Failed to find embedded resource: {resourceName}");
				return null;
			}

			byte[] buffer = new byte[stream.Length];
			var bytesRead = stream.Read(buffer, 0, buffer.Length);
			if (bytesRead != buffer.Length)
			{
				SfmLogger.LogError($"Failed to read the entire resource: {resourceName}");
				return null;
			}

			var texture = new Texture2D(100, 100, TextureFormat.RGBA32, false);
			if (!ImageConversion.LoadImage(texture, buffer))
			{
				SfmLogger.LogError($"Failed to load image from resource: {resourceName}");
				return null;
			}

			return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
		}
	}
}
