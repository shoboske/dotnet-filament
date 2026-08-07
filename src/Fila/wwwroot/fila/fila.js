document.addEventListener('alpine:init', () => {
  Alpine.data('filaTable', () => ({
    // Create/Edit forms open here via the server's `HX-Trigger: fila-modal-open` response
    // header (dispatched as a window CustomEvent by htmx); a successful save/delete closes it
    // the same way with `fila-modal-close`. See List.cshtml's @fila-modal-open.window listener.
    modalOpen: false,
  }))
})

// Alpine must survive htmx swaps: any x-data scope lives outside the element
// htmx replaces, but newly swapped-in markup still needs its directives bound. Guarded
// because a script-load failure of htmx must not take the rest of this file down with it
// (e.g. the notification listener below still has to register).
if (window.htmx) {
  htmx.onLoad((el) => {
    if (window.Alpine) Alpine.initTree(el)
  })
}

// Success toasts for Create/Update/Delete — server sends `HX-Trigger: {"fila-notify": {...}}`
// alongside `fila-modal-close`, htmx dispatches it as a bubbling CustomEvent, and this renders
// it into a fixed top-end stack matching Filament's default notification position.
const filaNotifyIcons = {
  success: '<path d="M20 6 9 17l-5-5"/>',
  danger: '<path d="M18 6 6 18M6 6l12 12"/>',
}

window.addEventListener('fila-notify', (e) => {
  const { title, color } = e.detail

  let container = document.getElementById('fila-notifications')
  if (!container) {
    container = document.createElement('div')
    container.id = 'fila-notifications'
    container.className = 'fi-no'
    document.body.appendChild(container)
  }

  const card = document.createElement('div')
  card.className = `fi-no-notification fi-no-notification-${color}`
  card.innerHTML =
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" ` +
    `stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="fi-icon fi-no-notification-icon" ` +
    `aria-hidden="true">${filaNotifyIcons[color] ?? filaNotifyIcons.success}</svg>` +
    `<p class="fi-no-notification-title"></p>`
  card.querySelector('.fi-no-notification-title').textContent = title

  container.prepend(card)
  requestAnimationFrame(() => card.classList.add('fi-visible'))

  setTimeout(() => {
    card.classList.remove('fi-visible')
    setTimeout(() => card.remove(), 200)
  }, 6000)
})
