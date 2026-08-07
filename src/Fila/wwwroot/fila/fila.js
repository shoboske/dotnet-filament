document.addEventListener('alpine:init', () => {
  Alpine.data('filaTable', () => ({
    // Create/Edit forms open here via the server's `HX-Trigger: fila-modal-open` response
    // header (dispatched as a window CustomEvent by htmx); a successful save/delete closes it
    // the same way with `fila-modal-close`. See List.cshtml's @fila-modal-open.window listener.
    modalOpen: false,
  }))
})

// Alpine must survive htmx swaps: any x-data scope lives outside the element
// htmx replaces, but newly swapped-in markup still needs its directives bound.
htmx.onLoad((el) => {
  if (window.Alpine) Alpine.initTree(el)
})
