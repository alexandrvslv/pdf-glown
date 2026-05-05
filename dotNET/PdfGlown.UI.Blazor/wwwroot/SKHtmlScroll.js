export class SKHtmlScroll {
    static init(elementId, moveAction, sizeAction, scrollAction) {
        if (!SKHtmlScroll.elements) {
            SKHtmlScroll.elements = new Map();
            SKHtmlScroll.observer = new ResizeObserver((entries) => {
                for (let entry of entries) {
                    SKHtmlScroll.sizeAllocated(entry.target);
                }
            });
        }
        const element = document.getElementById(elementId);
        var scrollElement = element;
        if (!scrollElement) {
            console.error(`No canvas element was provided.`);
            return;
        }
        SKHtmlScroll.elements.set(elementId, scrollElement);
        const view = new SKHtmlScroll(scrollElement, moveAction, sizeAction, scrollAction);
        scrollElement.SKHtmlScroll = view;
    }
    static getDPR() {
        return window.devicePixelRatio;
    }
    static deinit(elementId) {
        const element = SKHtmlScroll.elements.get(elementId);
        if (element) {
            SKHtmlScroll.elements.delete(elementId);
            element.SKHtmlScroll.deconstruct(element);
        }
    }
    static requestLock(elementId) {
        const element = SKHtmlScroll.elements.get(elementId);
        if (element)
            element.requestPointerLock();
    }
    static setCapture(elementId, pointerId) {
        const element = SKHtmlScroll.elements.get(elementId);
        if (element)
            element.setPointerCapture(pointerId);
    }
    static releaseCapture(elementId, pointerId) {
        const element = SKHtmlScroll.elements.get(elementId);
        if (element)
            element.releasePointerCapture(pointerId);
    }
    static changeCursor(elementId, cursorName) {
        const element = SKHtmlScroll.elements.get(elementId);
        if (element)
            element.style.cursor = cursorName;
    }
    static sizeAllocated(element) {
        element.SKHtmlScroll.sizeAction(element.clientWidth, element.clientHeight);
    }
    static unwrapp(jsObject) {
        return jsObject;
    }
    static eventArgsCreator(e) {
        return {
            "pointerId": e.pointerId,
            "button": SKHtmlScroll.getButton(e),
            "offsetX": e.offsetX,
            "offsetY": e.offsetY,
            "altKey": e.altKey,
            "ctrlKey": e.ctrlKey,
            "shiftKey": e.shiftKey,
            "metaKey": e.metaKey,
        };
    }
    static getButton(e) {
        return e.buttons == 1 ? 0 : e.buttons == 2 ? 2 : e.buttons == 4 ? 1 : -1;
    }
    static getKeyModifiers(e) {
        var result = 0;
        if (e.altKey)
            result |= 1;
        if (e.ctrlKey)
            result |= 2;
        if (e.shiftKey)
            result |= 4;
        if (e.metaKey)
            result |= 8;
        return result;
    }
    constructor(element, moveAction, sizeAction, scrollAction) {
        this.onPointerMove = (e) => {
            e.preventDefault();
            e.stopPropagation();
            this.moveAction([e.pointerId, SKHtmlScroll.getButton(e), e.offsetX, e.offsetY, SKHtmlScroll.getKeyModifiers(e)]);
        };
        this.onWeel = (e) => {
            e.preventDefault();
            e.stopPropagation();
            this.scrollAction(e.deltaY, e.ctrlKey);
        };
        this.moveAction = moveAction;
        this.sizeAction = sizeAction;
        this.scrollAction = scrollAction;
        element.addEventListener('pointermove', this.onPointerMove);
        element.addEventListener('wheel', this.onWeel);
        SKHtmlScroll.observer.observe(element);
    }
    deconstruct(element) {
        element.removeEventListener('pointermove', this.onPointerMove);
        element.removeEventListener('wheel', this.onWeel);
        SKHtmlScroll.observer.unobserve(element);
    }
}
//# sourceMappingURL=SKHtmlScroll.js.map