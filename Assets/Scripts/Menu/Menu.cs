using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
public class Menu : MonoBehaviour {
    private Stack<GameObject> _viewStack = new Stack<GameObject>();

    protected void PushView(GameObject view) {
        if(_viewStack.Count > 0) {
            _viewStack.Peek().SetActive(false);
        }
        view.SetActive(true);
        _viewStack.Push(view);
    }

    protected bool PopView() {
        if(_viewStack.TryPop(out var lastView)) {
            lastView.SetActive(false);
            if(_viewStack.Count > 0) {
                _viewStack.Peek().SetActive(true);
            }
            return true;
        }
        return false;
    }

    protected bool ViewStackEmpty() {
        return _viewStack.Count == 0;
    }

    public void ReturnFromView() {
        PopView();
        UnselectButton();
    }
    public void UnselectButton() {
        EventSystem.current.SetSelectedGameObject(null);
    }
}
