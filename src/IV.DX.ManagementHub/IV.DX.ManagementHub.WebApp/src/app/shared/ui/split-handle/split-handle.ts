import { ChangeDetectionStrategy, Component, output, signal } from '@angular/core';

/** Keyboard nudge per arrow key press, in pixels. */
const KEYBOARD_STEP = 16;

/**
 * Draggable divider between two panes. Emits the horizontal movement since the
 * previous event; the parent decides what to do with it.
 *
 * Angular Material has no splitter component, and CDK drag-and-drop moves the
 * element itself rather than reporting a delta, so this is plain pointer
 * handling. Pointer capture keeps the drag alive when the cursor outruns the
 * handle.
 */
@Component({
  selector: 'mh-split-handle',
  template: '<span class="split-handle__grip"></span>',
  styleUrl: './split-handle.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    role: 'separator',
    'aria-orientation': 'vertical',
    'aria-label': 'Изменить ширину панели',
    tabindex: '0',
    '[class.split-handle--active]': 'isDragging()',
    '(pointerdown)': 'onPointerDown($event)',
    '(pointermove)': 'onPointerMove($event)',
    '(pointerup)': 'onPointerUp($event)',
    '(pointercancel)': 'onPointerUp($event)',
    '(keydown.arrowleft)': 'moved.emit(-KEYBOARD_STEP)',
    '(keydown.arrowright)': 'moved.emit(KEYBOARD_STEP)',
  },
})
export class SplitHandle {
  /** Pixels moved since the previous event; negative means leftwards. */
  readonly moved = output<number>();

  protected readonly KEYBOARD_STEP = KEYBOARD_STEP;
  protected readonly isDragging = signal(false);

  private lastX = 0;

  protected onPointerDown(event: PointerEvent): void {
    (event.target as Element).setPointerCapture(event.pointerId);
    this.lastX = event.clientX;
    this.isDragging.set(true);
    // Stops the browser from selecting text across the panes while dragging.
    event.preventDefault();
  }

  protected onPointerMove(event: PointerEvent): void {
    if (!this.isDragging()) {
      return;
    }

    const delta = event.clientX - this.lastX;
    this.lastX = event.clientX;

    if (delta !== 0) {
      this.moved.emit(delta);
    }
  }

  protected onPointerUp(event: PointerEvent): void {
    if (!this.isDragging()) {
      return;
    }

    (event.target as Element).releasePointerCapture(event.pointerId);
    this.isDragging.set(false);
  }
}
