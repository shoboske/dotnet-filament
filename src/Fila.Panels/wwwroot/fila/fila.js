document.addEventListener('alpine:init', () => {
  Alpine.data('filaTable', () => ({
    // Create/Edit forms open here via the server's `HX-Trigger: fila-modal-open` response
    // header (dispatched as a window CustomEvent by htmx); a successful save/delete closes it
    // the same way with `fila-modal-close`. See List.cshtml's @fila-modal-open.window listener.
    modalOpen: false,
  }))

  // Selection count lives in a store rather than local x-data: the "N selected" indicator and
  // row checkboxes are inside #fila-table (rebuilt on every htmx swap), but the "Bulk actions"
  // dropdown trigger sits in the toolbar outside that swapped region — both need one shared,
  // swap-surviving source of truth.
  Alpine.store('bulkSelection', { count: 0 })
})

// Alpine must survive htmx swaps: any x-data scope lives outside the element
// htmx replaces, but newly swapped-in markup still needs its directives bound. Guarded
// because a script-load failure of htmx must not take the rest of this file down with it
// (e.g. the notification listener below still has to register).
if (window.htmx) {
  htmx.onLoad((el) => {
    if (window.Alpine) Alpine.initTree(el)
  })

  // A fresh #fila-table swap always renders its checkboxes unchecked, so any stale count
  // held by the toolbar's dropdown (outside the swapped region) must reset with it. Delegated
  // on document.body rather than htmx.on('#fila-table', ...) — that 3-arg form resolves the
  // selector immediately via querySelector and throws on any page without a table (Dashboard,
  // Login, ...), since fila.js loads site-wide, not just on List pages.
  htmx.on(document.body, 'htmx:afterSwap', (e) => {
    if (e.target.id === 'fila-table' && window.Alpine) Alpine.store('bulkSelection').count = 0
  })
}

// Success toasts for Create/Update/Delete — server sends `HX-Trigger: {"fila-notify": {...}}`
// alongside `fila-modal-close`, htmx dispatches it as a bubbling CustomEvent, and this renders
// it into a fixed inset-4 stack matching Notification::toEmbeddedHtml()'s markup: icon, title
// in a `.fi-no-notification-main`/`-text` wrapper, and an always-visible close button. Default
// icon per status mirrors Filament's HasIcon concern (check-circle for success, x-circle for danger).
const filaNotifyIcons = {
  success: '<circle cx="12" cy="12" r="9"/><path d="m8.5 12.5 2.5 2.5 4.5-5"/>',
  danger: '<circle cx="12" cy="12" r="9"/><path d="m9.5 9.5 5 5m0-5-5 5"/>',
}

const filaCloseIcon = '<path d="M18 6 6 18M6 6l12 12"/>'

function filaIconSvg(body, extraClass) {
  return (
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" ` +
    `stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" class="fi-icon ${extraClass}" ` +
    `aria-hidden="true">${body}</svg>`
  )
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
    filaIconSvg(filaNotifyIcons[color] ?? filaNotifyIcons.success, 'fi-no-notification-icon') +
    `<div class="fi-no-notification-main">` +
    `<div class="fi-no-notification-text"><h3 class="fi-no-notification-title"></h3></div>` +
    `</div>` +
    `<button type="button" class="fi-icon-btn fi-no-notification-close-btn" aria-label="Close">` +
    filaIconSvg(filaCloseIcon, '') +
    `</button>`
  card.querySelector('.fi-no-notification-title').textContent = title
  card.querySelector('.fi-no-notification-close-btn').addEventListener('click', () => dismiss())

  container.appendChild(card)
  requestAnimationFrame(() => card.classList.add('fi-visible'))

  const dismiss = () => {
    card.classList.remove('fi-visible')
    setTimeout(() => card.remove(), 300)
  }

  setTimeout(dismiss, 6000)
})
