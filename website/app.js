/**
 * ROSELIE'S BEAUTY LOUNGE — OFFICIAL WEBSITE LOGIC
 * Dynamic treatments, booking engine, custom bundle builder, boutique cart
 */

// ==========================================================================
// 1. DATA CATALOGS
// ==========================================================================

let SERVICES_DATA = [
  // Gluta Drips & Aesthetic Treatments
  {
    id: "srv-gluta-1",
    name: "Vitamin B12 and Vitamin C Push",
    category: "gluta",
    categoryLabel: "Gluta Push",
    price: 250,
    priceDisplay: "₱250.00",
    desc: "Energizing antioxidant and immunity boost push. Promotes cellular defense and vitality.",
    duration: "20 mins",
    packageAvailable: false
  },
  {
    id: "srv-gluta-2",
    name: "Collagen and Placenta Push",
    category: "gluta",
    categoryLabel: "Gluta Push",
    price: 400,
    priceDisplay: "₱400.00",
    desc: "Intensive anti-aging infusion for skin firmness, elasticity, and youthful glow.",
    duration: "25 mins",
    packageAvailable: false
  },
  {
    id: "srv-gluta-3",
    name: "Brightening Drip",
    category: "gluta",
    categoryLabel: "Gluta Drip",
    price: 599,
    priceDisplay: "₱599.00",
    desc: "Pure L-Glutathione and Vitamin C complex for visible skin brightening and detox.",
    duration: "45 mins",
    packageAvailable: true,
    packagePrice: 2995,
    packagePriceDisplay: "₱2,995.00"
  },
  {
    id: "srv-gluta-4",
    name: "Flawless Drip",
    category: "gluta",
    categoryLabel: "Gluta Drip",
    price: 899,
    priceDisplay: "₱899.00",
    desc: "High-dose Glutathione, Alpha Lipoic Acid, and multivitamins for uneven skin tone and radiance.",
    duration: "45 mins",
    packageAvailable: true,
    packagePrice: 4495,
    packagePriceDisplay: "₱4,495.00"
  },
  {
    id: "srv-gluta-5",
    name: "Ageless Drip",
    category: "gluta",
    categoryLabel: "Gluta Drip",
    price: 1499,
    priceDisplay: "₱1,499.00",
    desc: "Stem cell and collagen-rich IV infusion designed to combat fine lines and restore cellular energy.",
    duration: "60 mins",
    packageAvailable: true,
    packagePrice: 7495,
    packagePriceDisplay: "₱7,495.00"
  },
  {
    id: "srv-gluta-6",
    name: "Korean Drip",
    category: "gluta",
    categoryLabel: "Gluta Drip",
    price: 1699,
    priceDisplay: "₱1,699.00",
    desc: "Imported Seoul-formulated glass skin elixir with hyaluronic acid, coenzyme Q10, and high-potency gluta.",
    duration: "60 mins",
    packageAvailable: true,
    packagePrice: 8495,
    packagePriceDisplay: "₱8,495.00"
  },
  {
    id: "srv-gluta-7",
    name: "Japan Drip (White Platinum)",
    category: "gluta",
    categoryLabel: "Gluta Drip",
    price: 1899,
    priceDisplay: "₱1,899.00",
    desc: "The pinnacle of whitening aesthetics. Medical-grade Japanese formulation for luminous porcelain skin.",
    duration: "60 mins",
    packageAvailable: true,
    packagePrice: 9495,
    packagePriceDisplay: "₱9,495.00"
  },
  {
    id: "srv-gluta-8",
    name: "Slimming Shot",
    category: "gluta",
    categoryLabel: "Aesthetic Treatment",
    price: 1499,
    priceDisplay: "₱1,499.00",
    desc: "L-Carnitine and metabolism-enhancing lipotropic micro-shot targeting stubborn localized fat.",
    duration: "30 mins",
    packageAvailable: true,
    packagePrice: 7495,
    packagePriceDisplay: "₱7,495.00"
  },
  {
    id: "srv-gluta-9",
    name: "Lemon Bottle Fat Dissolving",
    category: "gluta",
    categoryLabel: "Aesthetic Treatment",
    price: 3000,
    priceDisplay: "₱3,000.00",
    desc: "Premium Korean lipolysis solution with riboflavin and lecithin. Price per bottle.",
    duration: "45 mins",
    packageAvailable: false
  },

  // Facial & Advanced Skin Care
  {
    id: "srv-facial-1",
    name: "Hydra Facial",
    category: "facial",
    categoryLabel: "Clinical Facial",
    price: 599,
    priceDisplay: "₱599.00",
    desc: "3-in-1 vortex vacuum cleansing, blackhead extraction, and antioxidant hydration infusion.",
    duration: "60 mins",
    packageAvailable: true,
    packagePrice: 2995,
    packagePriceDisplay: "₱2,995.00"
  },
  {
    id: "srv-facial-2",
    name: "Mermaid Facial",
    category: "facial",
    categoryLabel: "Clinical Facial",
    price: 899,
    priceDisplay: "₱899.00",
    desc: "Deep oxygenating marine algae facial with soothing lymphatic drainage and cryogenic rose globe massage.",
    duration: "75 mins",
    packageAvailable: true,
    packagePrice: 4495,
    packagePriceDisplay: "₱4,495.00"
  },
  {
    id: "srv-facial-3",
    name: "Microneedling (Collagen Induction)",
    category: "facial",
    categoryLabel: "Advanced Skin Care",
    price: 1499,
    priceDisplay: "₱1,499.00",
    desc: "Precision micro-channeling with hyaluronic serum for acne scars, enlarged pores, and textural renewal.",
    duration: "75 mins",
    packageAvailable: true,
    packagePrice: 7495,
    packagePriceDisplay: "₱7,495.00"
  },
  {
    id: "srv-facial-4",
    name: "Melasma & Pigment Treatment",
    category: "facial",
    categoryLabel: "Advanced Skin Care",
    price: 1399,
    priceDisplay: "₱1,399.00",
    desc: "Targeted dermal depigmentation facial using botanical tyrosinase inhibitors and gentle peeling.",
    duration: "60 mins",
    packageAvailable: true,
    packagePrice: 6995,
    packagePriceDisplay: "₱6,995.00"
  },
  {
    id: "srv-facial-5",
    name: "Radiofrequency (RF) Face Contouring",
    category: "facial",
    categoryLabel: "RF Contouring",
    price: 600,
    priceDisplay: "₱600.00",
    desc: "Non-invasive thermal collagen contraction for jawline sculpting and skin lifting.",
    duration: "45 mins",
    packageAvailable: true,
    packagePrice: 3000,
    packagePriceDisplay: "₱3,000.00"
  },
  {
    id: "srv-facial-6",
    name: "Radiofrequency (RF) Tummy Sculpt",
    category: "facial",
    categoryLabel: "Body Contouring",
    price: 1000,
    priceDisplay: "₱1,000.00",
    desc: "Multi-polar thermal body contouring targeting abdominal skin tightening and laxity.",
    duration: "50 mins",
    packageAvailable: true,
    packagePrice: 5000,
    packagePriceDisplay: "₱5,000.00"
  },
  {
    id: "srv-facial-7",
    name: "Radiofrequency (RF) Double Chin",
    category: "facial",
    categoryLabel: "RF Contouring",
    price: 300,
    priceDisplay: "₱300.00",
    desc: "Focused submental fat tightening to define the profile and reduce double chin sagging.",
    duration: "30 mins",
    packageAvailable: false
  },
  {
    id: "srv-facial-8",
    name: "Radiofrequency (RF) Eyebags",
    category: "facial",
    categoryLabel: "RF Contouring",
    price: 250,
    priceDisplay: "₱250.00",
    desc: "Gentle periorbital radiofrequency to depuff dark circles and tighten delicate eye contour skin.",
    duration: "25 mins",
    packageAvailable: false
  },

  // Laser Clinic
  {
    id: "srv-laser-1",
    name: "Pico Laser Face (Glow & Pore)",
    category: "laser",
    categoryLabel: "Pico Laser",
    price: 1199,
    priceDisplay: "₱1,199.00",
    desc: "Picosecond photo-acoustic laser pulses breaking down stubborn pigmentation and stimulating elastin.",
    duration: "45 mins",
    packageAvailable: true,
    packagePrice: 5995,
    packagePriceDisplay: "₱5,995.00"
  },
  {
    id: "srv-laser-2",
    name: "Pico Laser Underarms Lightening",
    category: "laser",
    categoryLabel: "Pico Laser",
    price: 899,
    priceDisplay: "₱899.00",
    desc: "Advanced brightening laser targeting friction hyperpigmentation and chicken skin texture.",
    duration: "30 mins",
    packageAvailable: true,
    packagePrice: 4495,
    packagePriceDisplay: "₱4,495.00"
  },
  {
    id: "srv-laser-3",
    name: "Diode Ice Laser Hair Removal (Underarms)",
    category: "laser",
    categoryLabel: "Diode Hair Removal",
    price: 1199,
    priceDisplay: "₱1,199.00",
    desc: "808nm sapphire contact cooling laser. Completely painless permanent hair follicle deactivation.",
    duration: "30 mins",
    packageAvailable: true,
    packagePrice: 5995,
    packagePriceDisplay: "₱5,995.00"
  },
  {
    id: "srv-laser-4",
    name: "Diode Laser Full Legs",
    category: "laser",
    categoryLabel: "Diode Hair Removal",
    price: 1499,
    priceDisplay: "₱1,499.00",
    desc: "Painless hair-free silky legs. Covers upper and lower legs with soothing aloe gel aftercare.",
    duration: "60 mins",
    packageAvailable: true,
    packagePrice: 7495,
    packagePriceDisplay: "₱7,495.00"
  },
  {
    id: "srv-laser-5",
    name: "Diode Laser Brazilian",
    category: "laser",
    categoryLabel: "Diode Hair Removal",
    price: 1999,
    priceDisplay: "₱1,999.00",
    desc: "Discreet, sanitized, and gentle full intimate hair removal in complete privacy.",
    duration: "45 mins",
    packageAvailable: true,
    packagePrice: 9995,
    packagePriceDisplay: "₱9,995.00"
  },
  {
    id: "srv-laser-6",
    name: "Tattoo Removal Laser",
    category: "laser",
    categoryLabel: "Laser Clinic",
    price: 499,
    priceDisplay: "Starts at ₱499.00",
    desc: "Targeted pigment shattering for cosmetic and permanent tattoos. Price varies by size.",
    duration: "30 mins",
    packageAvailable: false
  },
  {
    id: "srv-laser-7",
    name: "Electrocautery Warts Removal",
    category: "laser",
    categoryLabel: "Laser Clinic",
    price: 399,
    priceDisplay: "Starts at ₱399.00",
    desc: "Safe medical electrocautery removal with topical anesthetic cream for face, neck, or body.",
    duration: "45 mins",
    packageAvailable: false
  },

  // Couture Hair Studio
  {
    id: "srv-hair-1",
    name: "Deep Nourishing Hair Spa",
    category: "hair",
    categoryLabel: "Hair Treatment",
    price: 300,
    priceDisplay: "₱300 – ₱500",
    desc: "Hydrating hair mask with steaming treatment, scalp massage, and leave-in botanical serum.",
    duration: "45 mins",
    packageAvailable: false
  },
  {
    id: "srv-hair-2",
    name: "Couture Hair Color & Tone",
    category: "hair",
    categoryLabel: "Hair Styling",
    price: 500,
    priceDisplay: "₱500 – ₱1,000",
    desc: "Rich dimensional permanent or semi-permanent color formulated with conditioning oils.",
    duration: "90 mins",
    packageAvailable: false
  },
  {
    id: "srv-hair-3",
    name: "Brazilian Keratin Blowout",
    category: "hair",
    categoryLabel: "Hair Smoothing",
    price: 800,
    priceDisplay: "₱800 – ₱1,500",
    desc: "Eliminates frizz and seals cuticles for mirror-like shine and effortless manageability for up to 3 months.",
    duration: "120 mins",
    packageAvailable: false
  },
  {
    id: "srv-hair-4",
    name: "Classic Silk Rebond",
    category: "hair",
    categoryLabel: "Hair Rebonding",
    price: 1000,
    priceDisplay: "₱1,000 – ₱1,500",
    desc: "Pin-straight silky hair transformation with protein infusion to preserve strand resilience.",
    duration: "180 mins",
    packageAvailable: false
  },
  {
    id: "srv-hair-5",
    name: "L'Oréal Professionnel Rebond",
    category: "hair",
    categoryLabel: "Hair Rebonding",
    price: 2000,
    priceDisplay: "₱2,000 – ₱2,500",
    desc: "Luxury rebonding with genuine L'Oréal X-Tenso Oleoshape. Unmatched soft movement and natural shine.",
    duration: "210 mins",
    packageAvailable: false
  },
  {
    id: "srv-hair-6",
    name: "High-End Platinum Rebond",
    category: "hair",
    categoryLabel: "Hair Rebonding",
    price: 3500,
    priceDisplay: "₱3,500.00",
    desc: "The ultimate salon straightening ritual including bond rebuilder and luxury Moroccan oil finish.",
    duration: "240 mins",
    packageAvailable: false
  },

  // Nail Spa & Foot Care
  {
    id: "srv-nail-1",
    name: "Classic Manicure & Hand Care",
    category: "nails",
    categoryLabel: "Nail Care",
    price: 150,
    priceDisplay: "₱150.00",
    desc: "Nail shaping, cuticle grooming, hand massage, and regular high-shine polish.",
    duration: "35 mins",
    packageAvailable: false
  },
  {
    id: "srv-nail-2",
    name: "Classic Pedicure & Foot Care",
    category: "nails",
    categoryLabel: "Nail Care",
    price: 150,
    priceDisplay: "₱150.00",
    desc: "Foot soak, cuticle cleaning, heel buffing, and regular polish application.",
    duration: "40 mins",
    packageAvailable: false
  },
  {
    id: "srv-nail-3",
    name: "Gel Polish (Hands / Feet)",
    category: "nails",
    categoryLabel: "Gel Nails",
    price: 349,
    priceDisplay: "₱349 – ₱369",
    desc: "Long-lasting UV-cured gel polish with zero chipping for up to 3–4 weeks. Extensive color selection.",
    duration: "45 mins",
    packageAvailable: false
  },
  {
    id: "srv-nail-4",
    name: "Builder Gel Overlay",
    category: "nails",
    categoryLabel: "Gel Nails",
    price: 400,
    priceDisplay: "₱400 – ₱450",
    desc: "Reinforces natural nail plates with a durable crystal builder gel layer to prevent breakage.",
    duration: "60 mins",
    packageAvailable: false
  },
  {
    id: "srv-nail-5",
    name: "Soft Gel Nail Extensions",
    category: "nails",
    categoryLabel: "Nail Art",
    price: 449,
    priceDisplay: "Starts at ₱449.00",
    desc: "Flawless full-cover soft gel tips in almond, coffin, or square shapes tailored to your style.",
    duration: "75 mins",
    packageAvailable: false
  },
  {
    id: "srv-nail-6",
    name: "Aromatherapy Foot Spa with Massage",
    category: "nails",
    categoryLabel: "Foot Pampering",
    price: 450,
    priceDisplay: "₱450.00",
    desc: "Invigorating sea salt soak, dead skin callus scrub, lavender mask, and 20-min pressure-point foot massage.",
    duration: "60 mins",
    packageAvailable: false
  }
];

// ==========================================================================
// 2. STATE MANAGEMENT
// ==========================================================================

let customBundle = [];
let activeCategory = 'all';

// ==========================================================================
// 3. INITIALIZATION
// ==========================================================================

document.addEventListener('DOMContentLoaded', () => {
  // Sync custom catalog updated from admin console
  try {
    const customCat = localStorage.getItem('rbl_custom_catalog');
    if (customCat) {
      const parsed = JSON.parse(customCat);
      if (Array.isArray(parsed) && parsed.length > 0) {
        SERVICES_DATA = parsed;
      }
    }
  } catch (e) {}

  // Apply CMS Site Content (Hero, About, Pillars, Contact details)
  applySiteContent();

  renderServices();
  renderPackages();
  renderReviews();
  renderFaqs();
  renderBuilderItems();
  initBookingEngine();
  initFAQ();
  initNavigation();
  document.getElementById('floatingAdminPill')?.remove();
});

// ==========================================================================
// 4. RENDER SERVICES
// ==========================================================================

function renderServices() {
  const container = document.getElementById('servicesGrid');
  if (!container) return;

  const searchTerm = (document.getElementById('serviceSearchInput')?.value || '').toLowerCase().trim();

  const filtered = SERVICES_DATA.filter(s => {
    const matchesCat = (activeCategory === 'all' || s.category === activeCategory);
    const matchesSearch = s.name.toLowerCase().includes(searchTerm) || 
                          s.desc.toLowerCase().includes(searchTerm) || 
                          s.categoryLabel.toLowerCase().includes(searchTerm);
    return matchesCat && matchesSearch;
  });

  if (filtered.length === 0) {
    container.innerHTML = `
      <div style="grid-column: 1 / -1; text-align: center; padding: 48px; background: #fff; border-radius: 12px; border: 1px dashed #E9DADB;">
        <i class="fa-solid fa-magnifying-glass" style="font-size: 2rem; color: #C8797D; margin-bottom: 12px;"></i>
        <h4 style="font-size: 1.2rem; color: #344052;">No treatments found matching your criteria</h4>
        <p style="color: #77727B; margin-top: 6px;">Try adjusting your search terms or selecting another category.</p>
      </div>
    `;
    return;
  }

  container.innerHTML = filtered.map(s => `
    <div class="service-card" data-id="${s.id}">
      <div class="service-card-header">
        <div>
          <span class="sc-category">${s.categoryLabel}</span>
          <h3 class="sc-name">${s.name}</h3>
        </div>
        <div class="sc-price-box">
          <span class="sc-price-label">Price</span>
          <span class="sc-price">${s.priceDisplay}</span>
        </div>
      </div>
      
      <p class="sc-desc">${s.desc}</p>
      
      ${s.packageAvailable ? `
        <div class="sc-package-offer">
          <div>
            <strong>5+1 Session Offer:</strong> <span>${s.packagePriceDisplay}</span>
          </div>
          <span class="save-tag">1 Free Session</span>
        </div>
      ` : ''}

      <div class="sc-actions">
        <button class="btn btn-primary" onclick="quickBookService('${s.id}')">
          <i class="fa-regular fa-calendar-check"></i> Book Appointment
        </button>
      </div>
    </div>
  `).join('');
}

// Category filter tabs
document.getElementById('categoryTabs')?.addEventListener('click', (e) => {
  const btn = e.target.closest('.cat-pill');
  if (!btn) return;
  document.querySelectorAll('.cat-pill').forEach(b => b.classList.remove('active'));
  btn.classList.add('active');
  activeCategory = btn.dataset.category;
  renderServices();
});

// Search input
const searchInput = document.getElementById('serviceSearchInput');
const clearSearchBtn = document.getElementById('clearSearchBtn');

searchInput?.addEventListener('input', (e) => {
  if (clearSearchBtn) clearSearchBtn.style.display = e.target.value ? 'block' : 'none';
  renderServices();
});

clearSearchBtn?.addEventListener('click', () => {
  if (searchInput) searchInput.value = '';
  if (clearSearchBtn) clearSearchBtn.style.display = 'none';
  renderServices();
});

// ==========================================================================
// 5. RENDER 5+1 PACKAGES
// ==========================================================================

function renderPackages() {
  const container = document.getElementById('packagesGrid');
  if (!container) return;

  const pkgServices = SERVICES_DATA.filter(s => s.packageAvailable);

  container.innerHTML = pkgServices.map((p, idx) => `
    <div class="package-card ${idx === 1 ? 'featured' : ''}">
      ${idx === 1 ? '<span class="package-ribbon">MOST POPULAR</span>' : ''}
      <div class="pkg-header">
        <span class="pkg-subtitle">${p.categoryLabel} Signature</span>
        <h3 class="pkg-title">${p.name} (5+1 Sessions)</h3>
        <div class="pkg-price-row">
          <span class="pkg-price">${p.packagePriceDisplay}</span>
          <span class="pkg-terms">/ 6 total sessions</span>
        </div>
      </div>

      <div class="pkg-perks">
        <div class="pkg-perk-item">
          <i class="fa-solid fa-circle-check"></i>
          <span>Pay for 5 sessions, get the 6th session <strong>FREE</strong></span>
        </div>
        <div class="pkg-perk-item">
          <i class="fa-solid fa-circle-check"></i>
          <span>Valid for 12 months with flexible scheduling</span>
        </div>
        <div class="pkg-perk-item">
          <i class="fa-solid fa-circle-check"></i>
          <span>VIP priority booking slots &amp; private suite</span>
        </div>
        <div class="pkg-perk-item">
          <i class="fa-solid fa-circle-check"></i>
          <span>Complimentary collagen drink on every visit</span>
        </div>
      </div>

      <div class="pkg-actions">
        <button class="btn btn-gold btn-block" onclick="quickBookPackage('${p.id}')">
          <i class="fa-regular fa-calendar-check"></i> Avail 5+1 Package
        </button>
      </div>
    </div>
  `).join('');
}


// ==========================================================================
// 7. CUSTOM PAMPER BUNDLE BUILDER (CALCULATOR)
// ==========================================================================

function renderBuilderItems() {
  const container = document.getElementById('builderItemsGrid');
  if (!container) return;

  const selectable = SERVICES_DATA.filter(s => typeof s.price === 'number');

  container.innerHTML = selectable.map(s => {
    const isSelected = customBundle.some(item => item.id === s.id);
    return `
      <div class="builder-item-card ${isSelected ? 'selected' : ''}" onclick="toggleBundleItem('${s.id}')">
        <div class="bic-info">
          <h4>${s.name}</h4>
          <span>${s.categoryLabel} • ~${s.duration}</span>
        </div>
        <div style="display: flex; align-items: center; gap: 10px;">
          <span class="bic-price">₱${s.price.toLocaleString()}</span>
          <div class="bic-check"><i class="fa-solid fa-check"></i></div>
        </div>
      </div>
    `;
  }).join('');

  updateBundleSummary();
}

function toggleBundleItem(serviceId) {
  const service = SERVICES_DATA.find(s => s.id === serviceId);
  if (!service) return;

  const idx = customBundle.findIndex(item => item.id === serviceId);
  if (idx >= 0) {
    customBundle.splice(idx, 1);
  } else {
    customBundle.push(service);
  }

  renderBuilderItems();
}

function removeBundleItem(serviceId) {
  customBundle = customBundle.filter(i => i.id !== serviceId);
  renderBuilderItems();
}

function updateBundleSummary() {
  const list = document.getElementById('bundleSelectedList');
  const countBadge = document.getElementById('bundleCount');
  const regTotalElem = document.getElementById('bundleRegularTotal');
  const discountRow = document.getElementById('bundleDiscountRow');
  const discountElem = document.getElementById('bundleDiscountAmount');
  const finalTotalElem = document.getElementById('bundleFinalTotal');
  const bookBtn = document.getElementById('btnBookCustomBundle');

  if (!list) return;

  countBadge.textContent = `${customBundle.length} item${customBundle.length === 1 ? '' : 's'} selected`;

  if (customBundle.length === 0) {
    list.innerHTML = `
      <div class="empty-bundle-notice">
        <i class="fa-regular fa-hand-pointer"></i>
        <p>Click treatments on the left to add them to your personalized pamper package.</p>
      </div>
    `;
    regTotalElem.textContent = '₱0.00';
    discountRow.style.display = 'none';
    finalTotalElem.textContent = '₱0.00';
    bookBtn.disabled = true;
    return;
  }

  list.innerHTML = customBundle.map(item => `
    <div class="bundle-selected-item">
      <span>${item.name}</span>
      <div style="display: flex; align-items: center; gap: 8px;">
        <strong>₱${item.price.toLocaleString()}</strong>
        <button onclick="removeBundleItem('${item.id}')" title="Remove">&times;</button>
      </div>
    </div>
  `).join('');

  const regularSum = customBundle.reduce((acc, curr) => acc + curr.price, 0);
  let discount = 0;

  // 10% discount if 3 or more items
  if (customBundle.length >= 3) {
    discount = Math.round(regularSum * 0.10);
    discountRow.style.display = 'flex';
    discountElem.textContent = `-₱${discount.toLocaleString()}.00`;
  } else {
    discountRow.style.display = 'none';
  }

  const finalTotal = regularSum - discount;
  regTotalElem.textContent = `₱${regularSum.toLocaleString()}.00`;
  finalTotalElem.textContent = `₱${finalTotal.toLocaleString()}.00`;
  bookBtn.disabled = false;
}

document.getElementById('btnBookCustomBundle')?.addEventListener('click', () => {
  if (customBundle.length === 0) return;
  const names = customBundle.map(i => i.name).join(' + ');
  const categorySelect = document.getElementById('bookingCategory');
  const serviceSelect = document.getElementById('bookingService');

  categorySelect.value = 'gluta';
  categorySelect.dispatchEvent(new Event('change'));

  // Fill in client notes
  const notes = document.getElementById('clientNotes');
  if (notes) {
    notes.value = `Custom Pamper Bundle (${customBundle.length} services): ${names}`;
  }

  // Jump to booking
  document.querySelector('.nav-book-btn')?.click();
  showToast("Loaded your custom bundle into the booking form!", "gold");
});

// ==========================================================================
// 8. INTERACTIVE APPOINTMENT BOOKING ENGINE
// ==========================================================================

function initBookingEngine() {
  const catSelect = document.getElementById('bookingCategory');
  const srvSelect = document.getElementById('bookingService');
  const dateInput = document.getElementById('bookingDate');
  const preview = document.getElementById('servicePreviewCard');

  if (!catSelect || !srvSelect) return;

  // Set minimum date to today, default to tomorrow
  const today = new Date();
  const tomorrow = new Date();
  tomorrow.setDate(today.getDate() + 1);

  const pad = (n) => String(n).padStart(2, '0');
  const minDateStr = `${today.getFullYear()}-${pad(today.getMonth() + 1)}-${pad(today.getDate())}`;
  const defDateStr = `${tomorrow.getFullYear()}-${pad(tomorrow.getMonth() + 1)}-${pad(tomorrow.getDate())}`;

  dateInput.min = minDateStr;
  dateInput.value = defDateStr;

  // Render Time Slots
  generateTimeSlots();

  // Category change listener
  catSelect.addEventListener('change', () => {
    const val = catSelect.value;
    srvSelect.innerHTML = '<option value="">-- Select Service --</option>';

    if (!val) {
      srvSelect.disabled = true;
      preview.style.display = 'none';
      return;
    }

    srvSelect.disabled = false;
    let list = [];

    if (val === 'package') {
      list = SERVICES_DATA.filter(s => s.packageAvailable).map(s => ({
        id: 'pkg-' + s.id,
        name: `${s.name} (5+1 Sessions) — ${s.packagePriceDisplay}`,
        obj: s,
        isPackage: true
      }));
    } else {
      list = SERVICES_DATA.filter(s => s.category === val).map(s => ({
        id: s.id,
        name: `${s.name} (${s.priceDisplay})`,
        obj: s,
        isPackage: false
      }));
    }

    list.forEach(item => {
      const opt = document.createElement('option');
      opt.value = item.id;
      opt.textContent = item.name;
      srvSelect.appendChild(opt);
    });
  });

  // Service change listener
  srvSelect.addEventListener('change', () => {
    const val = srvSelect.value;
    if (!val) {
      preview.style.display = 'none';
      return;
    }

    let isPkg = val.startsWith('pkg-');
    let cleanId = isPkg ? val.replace('pkg-', '') : val;
    let s = SERVICES_DATA.find(x => x.id === cleanId);

    if (s) {
      preview.style.display = 'block';
      document.getElementById('spName').textContent = isPkg ? `${s.name} (5+1 Package)` : s.name;
      document.getElementById('spDesc').textContent = s.desc;
      document.getElementById('spPrice').textContent = isPkg ? s.packagePriceDisplay : s.priceDisplay;
    }
  });

  // Wizard Step Navigation
  document.getElementById('btnNextToStep2')?.addEventListener('click', () => {
    if (!srvSelect.value) {
      showToast("Please select a treatment service first.", "warning");
      srvSelect.focus();
      return;
    }
    goToStep(2);
  });

  document.getElementById('btnBackToStep1')?.addEventListener('click', () => goToStep(1));

  document.getElementById('btnNextToStep3')?.addEventListener('click', () => {
    const selectedSlot = document.getElementById('selectedTimeSlot').value;
    if (!dateInput.value) {
      showToast("Please select your appointment date.", "warning");
      dateInput.focus();
      return;
    }
    if (!selectedSlot) {
      showToast("Please choose an available time slot.", "warning");
      return;
    }

    // Populate review summary
    const srvText = srvSelect.options[srvSelect.selectedIndex]?.text || '';
    const dateFormatted = new Date(dateInput.value).toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' });
    document.getElementById('bsService').textContent = srvText;
    document.getElementById('bsDateTime').textContent = `${dateFormatted} at ${selectedSlot}`;
    
    // Total price
    let val = srvSelect.value;
    let isPkg = val.startsWith('pkg-');
    let s = SERVICES_DATA.find(x => x.id === (isPkg ? val.replace('pkg-', '') : val));
    document.getElementById('bsPrice').textContent = s ? (isPkg ? s.packagePriceDisplay : s.priceDisplay) : '₱0.00';

    goToStep(3);
  });

  document.getElementById('btnBackToStep2')?.addEventListener('click', () => goToStep(2));

  // Form Submission
  document.getElementById('bookingForm')?.addEventListener('submit', (e) => {
    e.preventDefault();

    const firstName = document.getElementById('clientFirstName').value.trim();
    const lastName = document.getElementById('clientLastName').value.trim();
    const phone = document.getElementById('clientPhone').value.trim();

    if (!firstName || !lastName || !phone) {
      showToast("Please complete your name and contact phone number.", "warning");
      return;
    }

    // Generate Booking Record
    const bookingCode = 'RBL-' + Math.floor(1000 + Math.random() * 9000);
    const serviceName = document.getElementById('bsService').textContent;
    const scheduleStr = document.getElementById('bsDateTime').textContent;
    const priceStr = document.getElementById('bsPrice').textContent;
    const specialist = document.getElementById('bookingSpecialist').options[document.getElementById('bookingSpecialist').selectedIndex].text;

    document.getElementById('confBookingId').textContent = bookingCode;
    document.getElementById('confClientName').textContent = `${firstName} ${lastName}`;
    document.getElementById('confService').textContent = serviceName;
    document.getElementById('confSchedule').textContent = scheduleStr;
    document.getElementById('confSpecialist').textContent = specialist;
    document.getElementById('confPrice').textContent = priceStr;

    // Setup WhatsApp link
    const waText = encodeURIComponent(`Hello Roselie's Beauty Lounge! I booked an appointment.\nCode: ${bookingCode}\nClient: ${firstName} ${lastName}\nService: ${serviceName}\nSchedule: ${scheduleStr}`);
    document.getElementById('btnShareWhatsApp').href = `https://wa.me/639171234567?text=${waText}`;

    // Setup ICS Calendar Download
    document.getElementById('btnDownloadIcs').onclick = () => generateIcsFile(bookingCode, serviceName, dateInput.value, document.getElementById('selectedTimeSlot').value);

    // Persist to Admin Database
    try {
      const email = document.getElementById('clientEmail')?.value.trim() || '';
      const notes = document.getElementById('clientNotes')?.value.trim() || '';
      const numPrice = parseFloat(priceStr.replace(/[^0-9.]/g, '')) || 0;
      const appRecord = {
        id: bookingCode,
        clientName: `${firstName} ${lastName}`,
        firstName: firstName,
        lastName: lastName,
        phone: phone,
        email: email,
        service: serviceName,
        date: dateInput.value,
        time: document.getElementById('selectedTimeSlot').value,
        schedule: scheduleStr,
        specialist: specialist,
        price: numPrice,
        priceDisplay: priceStr,
        status: "Pending",
        notes: notes,
        createdDate: new Date().toISOString()
      };
      const existingApps = JSON.parse(localStorage.getItem('rbl_appointments') || '[]');
      existingApps.push(appRecord);
      localStorage.setItem('rbl_appointments', JSON.stringify(existingApps));
    } catch (err) {
      console.warn("Storage sync:", err);
    }

    goToStep(4);
    showToast(`Appointment confirmed! Booking Voucher: ${bookingCode}`, "success");
  });

  document.getElementById('btnNewBooking')?.addEventListener('click', () => {
    document.getElementById('bookingForm').reset();
    document.getElementById('servicePreviewCard').style.display = 'none';
    dateInput.value = defDateStr;
    srvSelect.disabled = true;
    generateTimeSlots();
    goToStep(1);
  });
}

function generateTimeSlots() {
  const container = document.getElementById('timeSlotsGrid');
  if (!container) return;

  const slots = [
    "10:00 AM", "11:00 AM", "12:00 PM",
    "1:00 PM", "2:00 PM", "3:00 PM", "4:00 PM",
    "5:00 PM", "6:00 PM", "7:00 PM"
  ];

  container.innerHTML = slots.map((s, idx) => `
    <button type="button" class="time-slot-btn ${idx === 2 ? 'selected' : ''}" onclick="selectTimeSlot(this, '${s}')">
      ${s}
    </button>
  `).join('');

  document.getElementById('selectedTimeSlot').value = slots[2];
}

function selectTimeSlot(btn, slotText) {
  document.querySelectorAll('.time-slot-btn').forEach(b => b.classList.remove('selected'));
  btn.classList.add('selected');
  document.getElementById('selectedTimeSlot').value = slotText;
}

function goToStep(stepNum) {
  document.querySelectorAll('.form-step').forEach(s => s.classList.remove('active'));
  document.querySelectorAll('.step-indicator').forEach(ind => {
    const s = parseInt(ind.dataset.step);
    ind.classList.remove('active', 'completed');
    if (s === stepNum) ind.classList.add('active');
    if (s < stepNum) ind.classList.add('completed');
  });

  const target = document.getElementById(`formStep${stepNum}`);
  if (target) target.classList.add('active');
}

function quickBookService(serviceId) {
  const service = SERVICES_DATA.find(s => s.id === serviceId);
  if (!service) return;

  const catSelect = document.getElementById('bookingCategory');
  const srvSelect = document.getElementById('bookingService');

  catSelect.value = service.category;
  catSelect.dispatchEvent(new Event('change'));

  srvSelect.value = service.id;
  srvSelect.dispatchEvent(new Event('change'));

  document.querySelector('.nav-book-btn')?.click();
  showToast(`Selected "${service.name}" for your appointment!`, "success");
}

function quickBookPackage(serviceId) {
  const service = SERVICES_DATA.find(s => s.id === serviceId);
  if (!service) return;

  const catSelect = document.getElementById('bookingCategory');
  const srvSelect = document.getElementById('bookingService');

  catSelect.value = 'package';
  catSelect.dispatchEvent(new Event('change'));

  srvSelect.value = 'pkg-' + service.id;
  srvSelect.dispatchEvent(new Event('change'));

  document.querySelector('.nav-book-btn')?.click();
  showToast(`Selected 5+1 Package for "${service.name}"!`, "gold");
}

function generateIcsFile(bookingCode, serviceName, dateStr, timeStr) {
  const dtParts = dateStr.split('-');
  const dtFormatted = `${dtParts[0]}${dtParts[1]}${dtParts[2]}`;
  const icsData = [
    "BEGIN:VCALENDAR",
    "VERSION:2.0",
    "PRODID:-//Roselie's Beauty Lounge//NONSGML v1.0//EN",
    "BEGIN:VEVENT",
    `UID:${bookingCode}@roseliesbeautylounge.com`,
    `DTSTAMP:${dtFormatted}T090000Z`,
    `DTSTART:${dtFormatted}T100000Z`,
    `DTEND:${dtFormatted}T113000Z`,
    `SUMMARY:Roselie's Beauty Lounge - ${serviceName}`,
    `DESCRIPTION:Appointment Voucher: ${bookingCode}\\nTreatment: ${serviceName}\\nTime: ${timeStr}`,
    "LOCATION:Roselie's Beauty Lounge",
    "STATUS:CONFIRMED",
    "END:VEVENT",
    "END:VCALENDAR"
  ].join("\r\n");

  const blob = new Blob([icsData], { type: "text/calendar;charset=utf-8" });
  const link = document.createElement("a");
  link.href = URL.createObjectURL(blob);
  link.download = `Roselie-Appointment-${bookingCode}.ics`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
}

// ==========================================================================
// 10. FAQ ACCORDION
// ==========================================================================

function initFAQ() {
  const accordion = document.getElementById('faqAccordion');
  if (!accordion) return;

  accordion.addEventListener('click', (e) => {
    const btn = e.target.closest('.faq-question');
    if (!btn) return;

    const item = btn.parentElement;
    const isOpen = item.classList.contains('open');

    // Close others
    document.querySelectorAll('.faq-item').forEach(i => i.classList.remove('open'));

    // Toggle current
    if (!isOpen) {
      item.classList.add('open');
    }
  });
}

// ==========================================================================
// 11. NAVIGATION & MOBILE MENU
// ==========================================================================

function initNavigation() {
  const toggle = document.getElementById('mobileToggle');
  const nav = document.getElementById('mainNav');

  toggle?.addEventListener('click', () => {
    nav?.classList.toggle('active');
    toggle?.classList.toggle('active');
  });

  document.querySelectorAll('.nav-link').forEach(link => {
    link.addEventListener('click', () => {
      nav?.classList.remove('active');
      toggle?.classList.remove('active');
      document.querySelectorAll('.nav-link').forEach(l => l.classList.remove('active'));
      link.classList.add('active');
    });
  });

  // Close mobile drawer when tapping outside
  document.addEventListener('click', (e) => {
    if (nav?.classList.contains('active') && !nav.contains(e.target) && !toggle?.contains(e.target)) {
      nav.classList.remove('active');
      toggle?.classList.remove('active');
    }
  });

  // Contact form submission
  document.getElementById('contactForm')?.addEventListener('submit', (e) => {
    e.preventDefault();
    const name = document.getElementById('contactName').value.trim();
    const phone = document.getElementById('contactPhone').value.trim();
    const subject = document.getElementById('contactSubject')?.options[document.getElementById('contactSubject').selectedIndex]?.text || 'General Treatment Inquiry';
    const message = document.getElementById('contactMessage').value.trim();

    try {
      const msgRecord = {
        id: 'MSG-' + Math.floor(100 + Math.random() * 900),
        name: name,
        phone: phone,
        subject: subject,
        message: message,
        status: "New",
        date: new Date().toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
      };
      const existingMsgs = JSON.parse(localStorage.getItem('rbl_messages') || '[]');
      existingMsgs.push(msgRecord);
      localStorage.setItem('rbl_messages', JSON.stringify(existingMsgs));
    } catch (err) {
      console.warn("Storage sync:", err);
    }

    showToast(`Thank you, ${name}! Your inquiry has been sent to our concierge team.`, "success");
    e.target.reset();
  });
}

// ==========================================================================
// 12. TOAST NOTIFICATIONS
// ==========================================================================

function showToast(message, type = 'success') {
  const container = document.getElementById('toastContainer');
  if (!container) return;

  const toast = document.createElement('div');
  toast.className = `toast ${type}`;
  
  let icon = 'fa-check';
  if (type === 'warning') icon = 'fa-triangle-exclamation';
  if (type === 'gold') icon = 'fa-sparkles';

  toast.innerHTML = `
    <i class="fa-solid ${icon}"></i>
    <span>${message}</span>
  `;

  container.appendChild(toast);

  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transform = 'translateX(100%)';
    toast.style.transition = 'all 0.3s ease';
    setTimeout(() => toast.remove(), 300);
  }, 3500);
}

// ==========================================================================
// 13. CMS DYNAMIC CONTENT & REVIEWS / FAQS RENDERING
// ==========================================================================

const DEFAULT_TESTIMONIALS = [
  {
    id: "rev-1",
    name: "Bea P.",
    avatar: "BP",
    tag: "Regular Client • Japan Drip & Mermaid Facial",
    rating: 5,
    comment: "The Mermaid Facial and Japan Drip combo is life-changing! My skin was glowing for weeks. The lounge ambiance is so peaceful, clean, and luxurious. Definitely my holy grail beauty sanctuary!"
  },
  {
    id: "rev-2",
    name: "Katrina L.",
    avatar: "KL",
    tag: "Package Client • Diode Hair Removal 5+1",
    rating: 5,
    comment: "I availed of the Diode Hair Removal 5+1 package. By my 3rd session, hair growth was almost completely gone and painless! The nurse was so gentle and thorough. Super worth every peso!"
  },
  {
    id: "rev-3",
    name: "Maria G.",
    avatar: "MG",
    tag: "Salon Client • L'Oréal Rebond & Gel Overlay",
    rating: 5,
    comment: "Got my L'Oréal rebond and Gel nails done here. The stylists actually take care of your hair health without burning it. It's so silky smooth! Beautiful interior and courteous staff."
  }
];

const DEFAULT_FAQS = [
  {
    id: "faq-1",
    question: "How does the 5 + 1 Session Package work?",
    answer: "When you purchase any signature 5+1 package, you pay for 5 sessions and receive your 6th session completely free. Sessions can be scheduled whenever convenient over a 12-month period, with VIP priority booking slots."
  },
  {
    id: "faq-2",
    question: "Is the Diode Laser hair removal painful?",
    answer: "Our medical-grade Diode Ice Laser features active sapphire contact cooling (-5°C) that numbs the skin continuously throughout the pulse. Most clients describe it as feeling like a gentle cool glide with zero downtime."
  },
  {
    id: "faq-3",
    question: "How often can I receive a Gluta Drip or Push?",
    answer: "For initial skin radiance and antioxidant benefits, treatments are generally scheduled once a week or once every two weeks. Our registered nurses assess your hydration, blood pressure, and medical history before every drip."
  },
  {
    id: "faq-4",
    question: "What payment methods do you accept at the lounge?",
    answer: "We accept Cash (PHP), GCash, Maya, Bank Transfer (BDO, BPI, UnionBank), and all major Credit and Debit Cards (Visa, Mastercard, JCB)."
  },
  {
    id: "faq-5",
    question: "Do you accept walk-in clients?",
    answer: "Yes, walk-in clients are warmly welcomed! However, to avoid waiting times and secure your private treatment suite, we strongly recommend reserving your slot online via our booking engine."
  }
];

function applySiteContent() {
  try {
    const raw = localStorage.getItem('rbl_site_content');
    if (!raw) return;
    const c = JSON.parse(raw);

    const setTxt = (id, val) => {
      const el = document.getElementById(id);
      if (el && val !== undefined && val !== null) el.textContent = val;
    };

    const setHtml = (id, val) => {
      const el = document.getElementById(id);
      if (el && val !== undefined && val !== null) el.innerHTML = val;
    };

    // Hero
    if (c.heroTag) setHtml('heroTag', `<span class="sparkle">✦</span> ${c.heroTag} <span class="sparkle">✦</span>`);
    if (c.heroTitle) setHtml('heroTitle', c.heroTitle.replace(/\n/g, '<br>'));
    if (c.heroDesc) setTxt('heroDesc', c.heroDesc);
    setTxt('heroRatingVal', c.heroRatingVal);
    setTxt('heroRatingSub', c.heroRatingSub);
    setTxt('heroPromoBadgeTitle', c.heroPromoBadgeTitle);
    setTxt('heroPromoBadgeSub', c.heroPromoBadgeSub);

    // Hero Features
    setTxt('feat1Title', c.feat1Title);
    setTxt('feat1Sub', c.feat1Sub);
    setTxt('feat2Title', c.feat2Title);
    setTxt('feat2Sub', c.feat2Sub);
    setTxt('feat3Title', c.feat3Title);
    setTxt('feat3Sub', c.feat3Sub);

    // About / Experience
    setTxt('aboutYears', c.aboutYears);
    setTxt('aboutYearsText', c.aboutYearsText);
    setTxt('aboutSubtitle', c.aboutSubtitle);
    setTxt('aboutTitle', c.aboutTitle);
    setTxt('aboutPara1', c.aboutPara1);
    setTxt('aboutPara2', c.aboutPara2);

    // Pillars
    setTxt('pillar1Title', c.pillar1Title);
    setTxt('pillar1Desc', c.pillar1Desc);
    setTxt('pillar2Title', c.pillar2Title);
    setTxt('pillar2Desc', c.pillar2Desc);
    setTxt('pillar3Title', c.pillar3Title);
    setTxt('pillar3Desc', c.pillar3Desc);

    // Contact
    if (c.contactAddress) {
      setTxt('contactAddressDisplay', c.contactAddress);
      const footerAddr = document.getElementById('footerAddressDisplay');
      if (footerAddr) {
        footerAddr.innerHTML = `<i class="fa-solid fa-location-dot"></i> ${c.contactAddress}`;
      }
    }
    if (c.contactPhone) {
      const pl = document.getElementById('contactPhoneLink');
      if (pl) {
        pl.textContent = c.contactPhone;
        pl.href = `tel:${c.contactPhone.replace(/[^0-9+]/g, '')}`;
      }
      const bpl = document.getElementById('bookingPhoneLink');
      if (bpl) {
        bpl.innerHTML = `<i class="fa-solid fa-phone"></i> ${c.contactPhone}`;
        bpl.href = `tel:${c.contactPhone.replace(/[^0-9+]/g, '')}`;
      }
    }
    if (c.contactLandline) {
      const ll = document.getElementById('contactLandlineLink');
      if (ll) {
        ll.textContent = c.contactLandline;
        ll.href = `tel:${c.contactLandline.replace(/[^0-9+]/g, '')}`;
      }
    }
    if (c.contactHours) setHtml('contactHoursDisplay', c.contactHours.replace(/\n/g, '<br>'));
    if (c.contactEmail) {
      const el = document.getElementById('contactEmailLink');
      if (el) {
        el.textContent = c.contactEmail;
        el.href = `mailto:${c.contactEmail}`;
      }
    }

    // Socials
    if (c.socialFb) {
      document.getElementById('socialFbLink')?.setAttribute('href', c.socialFb);
      document.getElementById('bookingFbLink')?.setAttribute('href', c.socialFb);
    }
    if (c.socialIg) document.getElementById('socialIgLink')?.setAttribute('href', c.socialIg);
    if (c.socialTiktok) document.getElementById('socialTiktokLink')?.setAttribute('href', c.socialTiktok);
    if (c.socialWa) document.getElementById('socialWaLink')?.setAttribute('href', c.socialWa);
  } catch (err) {
    console.warn("Error applying site content:", err);
  }
}

function renderReviews() {
  const container = document.getElementById('reviewsGrid');
  if (!container) return;

  let reviews = DEFAULT_TESTIMONIALS;
  try {
    const raw = localStorage.getItem('rbl_testimonials');
    if (raw) {
      const parsed = JSON.parse(raw);
      if (Array.isArray(parsed) && parsed.length > 0) reviews = parsed;
    }
  } catch (e) {}

  container.innerHTML = reviews.map(r => {
    let stars = '';
    for (let i = 0; i < (r.rating || 5); i++) {
      stars += '<i class="fa-solid fa-star"></i>';
    }
    return `
      <div class="review-card">
        <div class="review-rating">${stars}</div>
        <p class="review-text">“${r.comment}”</p>
        <div class="reviewer">
          <div class="reviewer-avatar">${r.avatar || (r.name.slice(0, 2).toUpperCase())}</div>
          <div>
            <strong>${r.name}</strong>
            <span>${r.tag || 'Verified Client'}</span>
          </div>
        </div>
      </div>
    `;
  }).join('');
}

function renderFaqs() {
  const container = document.getElementById('faqAccordion');
  if (!container) return;

  let faqs = DEFAULT_FAQS;
  try {
    const raw = localStorage.getItem('rbl_faqs');
    if (raw) {
      const parsed = JSON.parse(raw);
      if (Array.isArray(parsed) && parsed.length > 0) faqs = parsed;
    }
  } catch (e) {}

  container.innerHTML = faqs.map((f, idx) => `
    <div class="faq-item ${idx === 0 ? 'open' : ''}">
      <button class="faq-question">
        <span>${f.question}</span>
        <i class="fa-solid fa-chevron-down"></i>
      </button>
      <div class="faq-answer">
        <p>${f.answer}</p>
      </div>
    </div>
  `).join('');
}

