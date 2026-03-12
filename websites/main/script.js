// Mobile nav toggle
document.getElementById('navToggle').addEventListener('click', function () {
    document.getElementById('navLinks').classList.toggle('active');
});

// Close mobile nav on link click
document.querySelectorAll('.nav-links a').forEach(function (link) {
    link.addEventListener('click', function () {
        document.getElementById('navLinks').classList.remove('active');
    });
});

// Navbar background on scroll
var nav = document.getElementById('nav');
window.addEventListener('scroll', function () {
    if (window.scrollY > 50) {
        nav.style.background = 'rgba(26,26,46,0.98)';
        nav.style.boxShadow = '0 2px 20px rgba(0,0,0,0.15)';
    } else {
        nav.style.background = 'rgba(26,26,46,0.95)';
        nav.style.boxShadow = 'none';
    }
});

// Simple scroll reveal
var observerOptions = { threshold: 0.1, rootMargin: '0px 0px -50px 0px' };
var observer = new IntersectionObserver(function (entries) {
    entries.forEach(function (entry) {
        if (entry.isIntersecting) {
            entry.target.style.opacity = '1';
            entry.target.style.transform = 'translateY(0)';
        }
    });
}, observerOptions);

document.querySelectorAll('.service-card, .contact-card, .trust-item').forEach(function (el) {
    el.style.opacity = '0';
    el.style.transform = 'translateY(20px)';
    el.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
    observer.observe(el);
});
