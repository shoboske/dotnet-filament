document.addEventListener('alpine:init', () => {
  // MVP: nothing yet — row selection lands with bulk actions (phase 2).
  Alpine.data('filaTable', () => ({}))
})

// Alpine must survive htmx swaps: any x-data scope lives outside the element
// htmx replaces, but newly swapped-in markup still needs its directives bound.
htmx.onLoad((el) => {
  if (window.Alpine) Alpine.initTree(el)
})
