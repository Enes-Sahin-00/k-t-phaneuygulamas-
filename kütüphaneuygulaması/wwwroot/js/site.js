// Modern JavaScript for enhanced user experience

// Global variables
let searchTimeout;
let currentPage = 1;

// DOM Content Loaded
document.addEventListener('DOMContentLoaded', function() {
    initializeApp();
});

// Initialize application
function initializeApp() {
    setupEventListeners();
    setupSearchFunctionality();
    setupCartFunctionality();
    setupFormValidation();
    setupResponsiveNavigation();
    setupLazyLoading();
    setupToastNotifications();
}

// Event listeners setup
function setupEventListeners() {
    // Search functionality
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.addEventListener('input', handleSearchInput);
        searchInput.addEventListener('keypress', handleSearchKeypress);
    }

    // Filter functionality
    const filterForm = document.getElementById('filterForm');
    if (filterForm) {
        filterForm.addEventListener('submit', handleFilterSubmit);
    }

    // Cart quantity updates
    const quantityInputs = document.querySelectorAll('.quantity-input');
    quantityInputs.forEach(input => {
        input.addEventListener('change', handleQuantityChange);
    });

    // Add to cart buttons
    const addToCartButtons = document.querySelectorAll('.add-to-cart-btn');
    addToCartButtons.forEach(button => {
        button.addEventListener('click', handleAddToCart);
    });

    // Remove from cart buttons
    const removeFromCartButtons = document.querySelectorAll('.remove-from-cart-btn');
    removeFromCartButtons.forEach(button => {
        button.addEventListener('click', handleRemoveFromCart);
    });

    // Pagination
    const paginationLinks = document.querySelectorAll('.page-link');
    paginationLinks.forEach(link => {
        link.addEventListener('click', handlePaginationClick);
    });

    // Modal functionality
    const modalTriggers = document.querySelectorAll('[data-bs-toggle="modal"]');
    modalTriggers.forEach(trigger => {
        trigger.addEventListener('click', handleModalTrigger);
    });

    // Tooltip initialization
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// Search functionality
function setupSearchFunctionality() {
    const searchInput = document.getElementById('searchInput');
    if (!searchInput) return;

    // Auto-complete functionality
    searchInput.addEventListener('input', function() {
        clearTimeout(searchTimeout);
        const query = this.value.trim();
        
        if (query.length >= 2) {
            searchTimeout = setTimeout(() => {
                fetchSearchSuggestions(query);
            }, 300);
        }
    });
}

// Handle search input
function handleSearchInput(event) {
    const query = event.target.value.trim();
    updateSearchResults(query);
}

// Handle search keypress
function handleSearchKeypress(event) {
    if (event.key === 'Enter') {
        event.preventDefault();
        performSearch();
    }
}

// Fetch search suggestions
async function fetchSearchSuggestions(query) {
    try {
        const response = await fetch(`/Books/Search?term=${encodeURIComponent(query)}`);
        const data = await response.json();
        
        if (data.suggestions && data.suggestions.length > 0) {
            showSearchSuggestions(data.suggestions);
        }
    } catch (error) {
        console.error('Search suggestions error:', error);
    }
}

// Show search suggestions
function showSearchSuggestions(suggestions) {
    let suggestionsContainer = document.getElementById('searchSuggestions');
    if (!suggestionsContainer) {
        suggestionsContainer = document.createElement('div');
        suggestionsContainer.id = 'searchSuggestions';
        suggestionsContainer.className = 'search-suggestions';
        document.getElementById('searchInput').parentNode.appendChild(suggestionsContainer);
    }

    suggestionsContainer.innerHTML = suggestions.map(suggestion => 
        `<div class="suggestion-item" onclick="selectSuggestion('${suggestion}')">${suggestion}</div>`
    ).join('');
}

// Select suggestion
function selectSuggestion(suggestion) {
    document.getElementById('searchInput').value = suggestion;
    document.getElementById('searchSuggestions').innerHTML = '';
    performSearch();
}

// Perform search
function performSearch() {
    const searchInput = document.getElementById('searchInput');
    const query = searchInput.value.trim();
    
    if (query) {
        const currentUrl = new URL(window.location);
        currentUrl.searchParams.set('searchTerm', query);
        currentUrl.searchParams.delete('page'); // Reset to first page
        window.location.href = currentUrl.toString();
    }
}

// Update search results
function updateSearchResults(query) {
    // This would be called for real-time search updates
    // For now, we'll just update the URL parameters
    const currentUrl = new URL(window.location);
    if (query) {
        currentUrl.searchParams.set('searchTerm', query);
    } else {
        currentUrl.searchParams.delete('searchTerm');
    }
    currentUrl.searchParams.delete('page');
    
    // Update URL without reloading
    window.history.replaceState({}, '', currentUrl.toString());
}

// Filter functionality
function handleFilterSubmit(event) {
    event.preventDefault();
    const formData = new FormData(event.target);
    const params = new URLSearchParams();
    
    for (let [key, value] of formData.entries()) {
        if (value) {
            params.append(key, value);
        }
    }
    
    window.location.href = `${window.location.pathname}?${params.toString()}`;
}

// Cart functionality
function setupCartFunctionality() {
    // Cart item quantity updates
    const quantityInputs = document.querySelectorAll('.quantity-input');
    quantityInputs.forEach(input => {
        input.addEventListener('change', handleQuantityChange);
    });
}

// Handle quantity change
async function handleQuantityChange(event) {
    const input = event.target;
    const cartId = input.dataset.cartId;
    const quantity = parseInt(input.value);
    
    if (quantity < 1) {
        input.value = 1;
        return;
    }
    
    try {
        const response = await fetch('/Cart/UpdateQuantity', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: `cartId=${cartId}&quantity=${quantity}`
        });
        
        if (response.ok) {
            updateCartTotal();
            showToast('Miktar güncellendi', 'success');
        } else {
            showToast('Miktar güncellenirken hata oluştu', 'error');
        }
    } catch (error) {
        console.error('Quantity update error:', error);
        showToast('Bir hata oluştu', 'error');
    }
}

// Handle add to cart
async function handleAddToCart(event) {
    event.preventDefault();
    const button = event.target;
    const bookId = button.dataset.bookId;
    const quantity = parseInt(button.dataset.quantity || 1);
    
    // Show loading state
    button.disabled = true;
    button.innerHTML = '<span class="loading-spinner"></span> Ekleniyor...';
    
    try {
        const response = await fetch('/Cart/Add', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: `bookId=${bookId}&quantity=${quantity}`
        });
        
        if (response.ok) {
            showToast('Kitap sepete eklendi', 'success');
            updateCartCount();
        } else {
            const errorData = await response.json();
            showToast(errorData.message || 'Kitap eklenirken hata oluştu', 'error');
        }
    } catch (error) {
        console.error('Add to cart error:', error);
        showToast('Bir hata oluştu', 'error');
    } finally {
        // Reset button state
        button.disabled = false;
        button.innerHTML = 'Sepete Ekle';
    }
}

// Handle remove from cart
async function handleRemoveFromCart(event) {
    event.preventDefault();
    const button = event.target;
    const cartId = button.dataset.cartId;
    
    if (!confirm('Bu ürünü sepetten kaldırmak istediğinizden emin misiniz?')) {
        return;
    }
    
    try {
        const response = await fetch('/Cart/Remove', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: `id=${cartId}`
        });
        
        if (response.ok) {
            // Remove the cart item from DOM
            const cartItem = button.closest('.cart-item');
            if (cartItem) {
                cartItem.remove();
                updateCartTotal();
                updateCartCount();
            }
            showToast('Ürün sepetten kaldırıldı', 'success');
        } else {
            showToast('Ürün kaldırılırken hata oluştu', 'error');
        }
    } catch (error) {
        console.error('Remove from cart error:', error);
        showToast('Bir hata oluştu', 'error');
    }
}

// Update cart total
function updateCartTotal() {
    const cartItems = document.querySelectorAll('.cart-item');
    let total = 0;
    
    cartItems.forEach(item => {
        const price = parseFloat(item.dataset.price || 0);
        const quantity = parseInt(item.querySelector('.quantity-input').value || 1);
        total += price * quantity;
    });
    
    const totalElement = document.getElementById('cartTotal');
    if (totalElement) {
        totalElement.textContent = total.toFixed(2) + ' ₺';
    }
}

// Update cart count
function updateCartCount() {
    const cartItems = document.querySelectorAll('.cart-item');
    const countElement = document.getElementById('cartCount');
    if (countElement) {
        countElement.textContent = cartItems.length;
    }
}

// Form validation
function setupFormValidation() {
    const forms = document.querySelectorAll('.needs-validation');
    forms.forEach(form => {
        form.addEventListener('submit', function(event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        });
    });
}

// Responsive navigation
function setupResponsiveNavigation() {
    const navbarToggler = document.querySelector('.navbar-toggler');
    const navbarCollapse = document.querySelector('.navbar-collapse');
    
    if (navbarToggler && navbarCollapse) {
        navbarToggler.addEventListener('click', function() {
            navbarCollapse.classList.toggle('show');
        });
        
        // Close mobile menu when clicking outside
        document.addEventListener('click', function(event) {
            if (!navbarToggler.contains(event.target) && !navbarCollapse.contains(event.target)) {
                navbarCollapse.classList.remove('show');
            }
        });
    }
}

// Lazy loading for images
function setupLazyLoading() {
    const images = document.querySelectorAll('img[data-src]');
    const imageObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                img.src = img.dataset.src;
                img.classList.remove('lazy');
                observer.unobserve(img);
            }
        });
    });
    
    images.forEach(img => imageObserver.observe(img));
}

// Toast notifications
function setupToastNotifications() {
    // Create toast container if it doesn't exist
    if (!document.getElementById('toastContainer')) {
        const toastContainer = document.createElement('div');
        toastContainer.id = 'toastContainer';
        toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
        toastContainer.style.zIndex = '9999';
        document.body.appendChild(toastContainer);
    }
}

// Show toast notification
function showToast(message, type = 'info') {
    const toastContainer = document.getElementById('toastContainer');
    const toastId = 'toast-' + Date.now();
    
    const toastHtml = `
        <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="toast-header">
                <strong class="me-auto">${type === 'success' ? 'Başarı' : type === 'error' ? 'Hata' : 'Bilgi'}</strong>
                <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        </div>
    `;
    
    toastContainer.insertAdjacentHTML('beforeend', toastHtml);
    
    const toastElement = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastElement, {
        autohide: true,
        delay: 3000
    });
    
    toast.show();
    
    // Remove toast element after it's hidden
    toastElement.addEventListener('hidden.bs.toast', function() {
        toastElement.remove();
    });
}

// Pagination handling
function handlePaginationClick(event) {
    event.preventDefault();
    const link = event.target;
    const page = link.dataset.page;
    
    if (page) {
        const currentUrl = new URL(window.location);
        currentUrl.searchParams.set('page', page);
        window.location.href = currentUrl.toString();
    }
}

// Modal handling
function handleModalTrigger(event) {
    const target = event.target.dataset.bsTarget;
    const modal = document.querySelector(target);
    
    if (modal) {
        const modalInstance = new bootstrap.Modal(modal);
        modalInstance.show();
    }
}

// Get anti-forgery token
function getAntiForgeryToken() {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenElement ? tokenElement.value : '';
}

// Utility functions
function formatPrice(price) {
    return new Intl.NumberFormat('tr-TR', {
        style: 'currency',
        currency: 'TRY'
    }).format(price);
}

function formatDate(dateString) {
    return new Date(dateString).toLocaleDateString('tr-TR');
}

// Debounce function
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Throttle function
function throttle(func, limit) {
    let inThrottle;
    return function() {
        const args = arguments;
        const context = this;
        if (!inThrottle) {
            func.apply(context, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    };
}

// Export functions for global use
window.libraryApp = {
    showToast,
    formatPrice,
    formatDate,
    performSearch,
    handleAddToCart,
    handleRemoveFromCart,
    updateCartTotal,
    updateCartCount
};
