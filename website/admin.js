/**
 * ROSELIE'S BEAUTY LOUNGE — OFFICIAL ADMIN PORTAL JAVASCRIPT
 * Real-time appointment management, orders fulfillment, catalog pricing, CSV export
 */

// ==========================================================================
// 1. DEFAULT SEED DATA & INITIAL CATALOG
// ==========================================================================

const DEFAULT_SERVICES = [
  // Gluta Drips & Aesthetic Treatments
  { id: "srv-gluta-1", name: "Vitamin B12 and Vitamin C Push", category: "gluta", categoryLabel: "Gluta Push", price: 250, priceDisplay: "₱250.00", desc: "Energizing antioxidant and immunity boost push.", duration: "20 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },
  { id: "srv-gluta-2", name: "Collagen and Placenta Push", category: "gluta", categoryLabel: "Gluta Push", price: 400, priceDisplay: "₱400.00", desc: "Intensive anti-aging infusion for skin firmness and elasticity.", duration: "25 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },
  { id: "srv-gluta-3", name: "Brightening Drip", category: "gluta", categoryLabel: "Gluta Drip", price: 599, priceDisplay: "₱599.00", desc: "Pure L-Glutathione and Vitamin C complex for visible skin brightening.", duration: "45 mins", packageAvailable: true, packagePrice: 2995, packagePriceDisplay: "₱2,995.00" },
  { id: "srv-gluta-4", name: "Flawless Drip", category: "gluta", categoryLabel: "Gluta Drip", price: 899, priceDisplay: "₱899.00", desc: "High-dose Glutathione, Alpha Lipoic Acid, and multivitamins.", duration: "45 mins", packageAvailable: true, packagePrice: 4495, packagePriceDisplay: "₱4,495.00" },
  { id: "srv-gluta-5", name: "Ageless Drip", category: "gluta", categoryLabel: "Gluta Drip", price: 1499, priceDisplay: "₱1,499.00", desc: "Stem cell and collagen-rich IV infusion designed to combat fine lines.", duration: "60 mins", packageAvailable: true, packagePrice: 7495, packagePriceDisplay: "₱7,495.00" },
  { id: "srv-gluta-6", name: "Korean Drip", category: "gluta", categoryLabel: "Gluta Drip", price: 1699, priceDisplay: "₱1,699.00", desc: "Imported Seoul-formulated glass skin elixir with hyaluronic acid.", duration: "60 mins", packageAvailable: true, packagePrice: 8495, packagePriceDisplay: "₱8,495.00" },
  { id: "srv-gluta-7", name: "Japan Drip (White Platinum)", category: "gluta", categoryLabel: "Gluta Drip", price: 1899, priceDisplay: "₱1,899.00", desc: "The pinnacle of whitening aesthetics. Medical-grade Japanese formulation.", duration: "60 mins", packageAvailable: true, packagePrice: 9495, packagePriceDisplay: "₱9,495.00" },
  { id: "srv-gluta-8", name: "Slimming Shot", category: "gluta", categoryLabel: "Aesthetic Treatment", price: 1499, priceDisplay: "₱1,499.00", desc: "L-Carnitine and metabolism-enhancing lipotropic micro-shot.", duration: "30 mins", packageAvailable: true, packagePrice: 7495, packagePriceDisplay: "₱7,495.00" },
  { id: "srv-gluta-9", name: "Lemon Bottle Fat Dissolving", category: "gluta", categoryLabel: "Aesthetic Treatment", price: 3000, priceDisplay: "₱3,000.00", desc: "Premium Korean lipolysis solution with riboflavin and lecithin.", duration: "45 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },

  // Facial & Advanced Skin Care
  { id: "srv-facial-1", name: "Hydra Facial", category: "facial", categoryLabel: "Clinical Facial", price: 599, priceDisplay: "₱599.00", desc: "3-in-1 vortex vacuum cleansing, blackhead extraction, hydration infusion.", duration: "60 mins", packageAvailable: true, packagePrice: 2995, packagePriceDisplay: "₱2,995.00" },
  { id: "srv-facial-2", name: "Mermaid Facial", category: "facial", categoryLabel: "Clinical Facial", price: 899, priceDisplay: "₱899.00", desc: "Deep oxygenating marine algae facial with soothing rose globe massage.", duration: "75 mins", packageAvailable: true, packagePrice: 4495, packagePriceDisplay: "₱4,495.00" },
  { id: "srv-facial-3", name: "Microneedling (Collagen Induction)", category: "facial", categoryLabel: "Advanced Skin Care", price: 1499, priceDisplay: "₱1,499.00", desc: "Precision micro-channeling for acne scars and pore reduction.", duration: "75 mins", packageAvailable: true, packagePrice: 7495, packagePriceDisplay: "₱7,495.00" },
  { id: "srv-facial-4", name: "Melasma & Pigment Treatment", category: "facial", categoryLabel: "Advanced Skin Care", price: 1399, priceDisplay: "₱1,399.00", desc: "Targeted dermal depigmentation with botanical tyrosinase inhibitors.", duration: "60 mins", packageAvailable: true, packagePrice: 6995, packagePriceDisplay: "₱6,995.00" },
  { id: "srv-facial-5", name: "Radiofrequency (RF) Face Contouring", category: "facial", categoryLabel: "RF Contouring", price: 600, priceDisplay: "₱600.00", desc: "Non-invasive thermal collagen contraction for jawline sculpting.", duration: "45 mins", packageAvailable: true, packagePrice: 3000, packagePriceDisplay: "₱3,000.00" },
  { id: "srv-facial-6", name: "Radiofrequency (RF) Tummy Sculpt", category: "facial", categoryLabel: "Body Contouring", price: 1000, priceDisplay: "₱1,000.00", desc: "Multi-polar thermal body contouring for abdominal skin tightening.", duration: "50 mins", packageAvailable: true, packagePrice: 5000, packagePriceDisplay: "₱5,000.00" },
  { id: "srv-facial-7", name: "Radiofrequency (RF) Double Chin", category: "facial", categoryLabel: "RF Contouring", price: 300, priceDisplay: "₱300.00", desc: "Focused submental fat tightening to define the profile.", duration: "30 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },
  { id: "srv-facial-8", name: "Radiofrequency (RF) Eyebags", category: "facial", categoryLabel: "RF Contouring", price: 250, priceDisplay: "₱250.00", desc: "Gentle periorbital radiofrequency to depuff dark circles.", duration: "25 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },

  // Laser Clinic
  { id: "srv-laser-1", name: "Pico Laser Face (Glow & Pore)", category: "laser", categoryLabel: "Pico Laser", price: 1199, priceDisplay: "₱1,199.00", desc: "Picosecond photo-acoustic laser pulses breaking down stubborn pigmentation.", duration: "45 mins", packageAvailable: true, packagePrice: 5995, packagePriceDisplay: "₱5,995.00" },
  { id: "srv-laser-2", name: "Pico Laser Underarms Lightening", category: "laser", categoryLabel: "Pico Laser", price: 899, priceDisplay: "₱899.00", desc: "Advanced brightening laser targeting friction hyperpigmentation.", duration: "30 mins", packageAvailable: true, packagePrice: 4495, packagePriceDisplay: "₱4,495.00" },
  { id: "srv-laser-3", name: "Diode Ice Laser Hair Removal (Underarms)", category: "laser", categoryLabel: "Diode Hair Removal", price: 1199, priceDisplay: "₱1,199.00", desc: "808nm sapphire contact cooling laser. Painless permanent reduction.", duration: "30 mins", packageAvailable: true, packagePrice: 5995, packagePriceDisplay: "₱5,995.00" },
  { id: "srv-laser-4", name: "Diode Laser Full Legs", category: "laser", categoryLabel: "Diode Hair Removal", price: 1499, priceDisplay: "₱1,499.00", desc: "Painless hair-free silky legs. Covers upper and lower legs with cooling gel.", duration: "60 mins", packageAvailable: true, packagePrice: 7495, packagePriceDisplay: "₱7,495.00" },
  { id: "srv-laser-5", name: "Diode Laser Brazilian", category: "laser", categoryLabel: "Diode Hair Removal", price: 1999, priceDisplay: "₱1,999.00", desc: "Discreet, sanitized, and gentle full intimate hair removal in privacy.", duration: "45 mins", packageAvailable: true, packagePrice: 9995, packagePriceDisplay: "₱9,995.00" },
  { id: "srv-laser-6", name: "Tattoo Removal Laser", category: "laser", categoryLabel: "Laser Clinic", price: 499, priceDisplay: "Starts at ₱499.00", desc: "Targeted pigment shattering for cosmetic and permanent tattoos.", duration: "30 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },
  { id: "srv-laser-7", name: "Electrocautery Warts Removal", category: "laser", categoryLabel: "Laser Clinic", price: 399, priceDisplay: "Starts at ₱399.00", desc: "Safe medical electrocautery removal with topical anesthetic cream.", duration: "30 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },

  // Hair Studio
  { id: "srv-hair-1", name: "Deep Nourishing Hair Spa", category: "hair", categoryLabel: "Hair Treatment", price: 350, priceDisplay: "₱350.00", desc: "Hydrating hair mask with steaming treatment, scalp massage, and serum.", duration: "45 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },
  { id: "srv-hair-2", name: "Couture Hair Color & Tone", category: "hair", categoryLabel: "Hair Styling", price: 750, priceDisplay: "₱750.00", desc: "Rich dimensional permanent or semi-permanent color formulated with conditioning oils.", duration: "90 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },
  { id: "srv-hair-3", name: "Brazilian Keratin Blowout", category: "hair", categoryLabel: "Hair Smoothing", price: 1150, priceDisplay: "₱1,150.00", desc: "Eliminates frizz and seals cuticles for mirror-like shine and 3 months manageability.", duration: "120 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },
  { id: "srv-hair-4", name: "Classic Silk Rebond", category: "hair", categoryLabel: "Hair Rebonding", price: 1250, priceDisplay: "₱1,250.00", desc: "Pin-straight silky hair transformation with protein infusion.", duration: "180 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },
  { id: "srv-hair-5", name: "L'Oréal Professionnel Rebond", category: "hair", categoryLabel: "Hair Rebonding", price: 2250, priceDisplay: "₱2,250.00", desc: "Luxury rebonding with genuine L'Oréal X-Tenso Oleoshape. Unmatched soft movement.", duration: "180 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },
  { id: "srv-hair-6", name: "High-End Platinum Rebond", category: "hair", categoryLabel: "Hair Rebonding", price: 3500, priceDisplay: "₱3,500.00", desc: "The ultimate salon straightening ritual including bond rebuilder and Moroccan oil.", duration: "210 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },

  // Nail Spa & Foot Care
  { id: "srv-nail-1", name: "Classic Manicure & Hand Care", category: "nails", categoryLabel: "Nail Care", price: 150, priceDisplay: "₱150.00", desc: "Nail shaping, cuticle grooming, hand massage, and regular high-shine polish.", duration: "30 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },
  { id: "srv-nail-2", name: "Classic Pedicure & Foot Care", category: "nails", categoryLabel: "Nail Care", price: 150, priceDisplay: "₱150.00", desc: "Foot soak, cuticle cleaning, heel buffing, and regular polish application.", duration: "35 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },
  { id: "srv-nail-3", name: "Gel Polish (Hands / Feet)", category: "nails", categoryLabel: "Gel Nails", price: 350, priceDisplay: "₱350.00", desc: "Long-lasting UV-cured gel polish with zero chipping for up to 3–4 weeks.", duration: "45 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },
  { id: "srv-nail-4", name: "Builder Gel Overlay", category: "nails", categoryLabel: "Gel Nails", price: 420, priceDisplay: "₱420.00", desc: "Reinforces natural nail plates with a durable crystal builder gel layer.", duration: "50 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },
  { id: "srv-nail-5", name: "Soft Gel Nail Extensions", category: "nails", categoryLabel: "Nail Art", price: 449, priceDisplay: "₱449.00", desc: "Flawless full-cover soft gel tips in almond, coffin, or square shapes.", duration: "60 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" },
  { id: "srv-nail-6", name: "Aromatherapy Foot Spa with Massage", category: "nails", categoryLabel: "Foot Pampering", price: 450, priceDisplay: "₱450.00", desc: "Invigorating sea salt soak, dead skin callus scrub, lavender mask, and 20-min pressure-point massage.", duration: "60 mins", packageAvailable: false, packagePrice: 0, packagePriceDisplay: "N/A" }
];

const SEED_APPOINTMENTS = [
  {
    id: "RBL-8921",
    clientName: "Maria Santos",
    firstName: "Maria",
    lastName: "Santos",
    phone: "0917 123 4567",
    email: "maria.santos@gmail.com",
    service: "Hydra Facial",
    date: "2026-10-12",
    time: "2:00 PM",
    schedule: "Mon, Oct 12, 2026 at 2:00 PM",
    specialist: "Nurse Roselie (Lead Aesthetician)",
    price: 599,
    priceDisplay: "₱599.00",
    status: "Confirmed",
    notes: "First time client. Wants pore extraction and hydration.",
    createdDate: "2026-10-09 08:30"
  },
  {
    id: "RBL-4512",
    clientName: "Bea Pascual",
    firstName: "Bea",
    lastName: "Pascual",
    phone: "0918 222 3344",
    email: "bea.p@yahoo.com",
    service: "Japan Drip (White Platinum) (5+1 Package)",
    date: "2026-10-13",
    time: "11:00 AM",
    schedule: "Tue, Oct 13, 2026 at 11:00 AM",
    specialist: "Therapist Camille (Skin & Drip Expert)",
    price: 9495,
    priceDisplay: "₱9,495.00",
    status: "Pending",
    notes: "Availing signature 5+1 package with VIP suite reservation.",
    createdDate: "2026-10-09 07:15"
  },
  {
    id: "RBL-7730",
    clientName: "Katrina Lim",
    firstName: "Katrina",
    lastName: "Lim",
    phone: "0920 555 6677",
    email: "katrinalim@outlook.com",
    service: "Diode Ice Laser Hair Removal (Underarms)",
    date: "2026-10-09",
    time: "4:00 PM",
    schedule: "Today at 4:00 PM",
    specialist: "Dr. Sarah (Laser & Skin Specialist)",
    price: 1199,
    priceDisplay: "₱1,199.00",
    status: "Completed",
    notes: "Session 2 of 6 completed. Very satisfied with results.",
    createdDate: "2026-10-08 14:00"
  }
];

const SEED_ORDERS = [
  {
    id: "RBL-ORD-88214",
    customerName: "Anna Marie Gomez",
    phone: "0917 888 1234",
    address: "Commercial Center",
    items: [
      { name: "Roselie's Glutathione Glow Capsules", qty: 1, price: 1499 },
      { name: "UV Defense Fluid SPF 50+", qty: 1, price: 850 }
    ],
    itemsSummary: "1x Glow Capsules, 1x UV Defense Fluid SPF 50+",
    total: 2349,
    totalDisplay: "₱2,349.00",
    paymentMethod: "GCash",
    status: "Processing",
    date: "2026-10-09 08:10"
  },
  {
    id: "RBL-ORD-93012",
    customerName: "Patricia Diaz",
    phone: "0918 555 7890",
    address: "Store Pickup at Lounge Counter",
    items: [
      { name: "Triple HA Hydrating Serum", qty: 1, price: 899 }
    ],
    itemsSummary: "1x Triple HA Hydrating Serum",
    total: 899,
    totalDisplay: "₱899.00",
    paymentMethod: "Pay at Lounge Counter",
    status: "Ready for Pickup",
    date: "2026-10-08 16:45"
  }
];

const SEED_MESSAGES = [
  {
    id: "MSG-101",
    name: "Christine Reyes",
    phone: "0917 000 1111",
    subject: "Bridal / Group Pamper Packages",
    message: "Hi! Inquiring about packages for a bridal party of 6 pax this coming November. We want Mermaid Facials and Gel Nails.",
    status: "New",
    date: "2026-10-08 19:20"
  },
  {
    id: "MSG-102",
    name: "Danica Cruz",
    phone: "0922 333 4444",
    subject: "Laser & Skin Consultation",
    message: "Does Pico laser help with dark acne scars and active redness? How many sessions are recommended?",
    status: "Contacted",
    date: "2026-10-08 15:10"
  }
];

// ==========================================================================
// 2. DATA STORAGE ACCESSORS
// ==========================================================================

function getStorage(key, fallback) {
  try {
    const raw = localStorage.getItem(key);
    if (!raw) {
      localStorage.setItem(key, JSON.stringify(fallback));
      return (typeof fallback === 'object' && fallback !== null && !Array.isArray(fallback))
        ? { ...fallback }
        : (Array.isArray(fallback) ? [...fallback] : fallback);
    }
    const parsed = JSON.parse(raw);

    // Deep merge for settings & CMS objects (e.g. rbl_site_content):
    // Preserves all user customized entries (location address, phone, socials, custom text)
    // while seamlessly incorporating any new schema properties added during app updates.
    if (typeof fallback === 'object' && fallback !== null && !Array.isArray(fallback) &&
        typeof parsed === 'object' && parsed !== null && !Array.isArray(parsed)) {
      return { ...fallback, ...parsed };
    }

    // Smart merge for treatment catalog (rbl_custom_catalog):
    // Preserves all user custom pricing, package toggles, and newly created custom treatments,
    // while seamlessly appending any brand new official treatments added to DEFAULT_SERVICES in updates.
    if (key === 'rbl_custom_catalog' && Array.isArray(fallback) && Array.isArray(parsed) && parsed.length > 0) {
      const merged = [...parsed];
      fallback.forEach(defItem => {
        if (!merged.some(m => m.id === defItem.id)) {
          merged.push(defItem);
        }
      });
      return merged;
    }

    return parsed;
  } catch (e) {
    return fallback;
  }
}

function setStorage(key, val) {
  localStorage.setItem(key, JSON.stringify(val));
}

const DEFAULT_SITE_CONTENT = {
  heroTag: "✦ Luxury Aesthetics & Beauty Lounge ✦",
  heroTitle: "Where Elegance Meets <br><span class=\"text-gradient-gold\">Aesthetic Perfection</span>",
  heroDesc: "Step into a sanctuary of transformative wellness. Experience medical-grade gluta drips, pain-free diode lasers, advanced hydro-facials, and couture salon treatments tailored to unveil your luminous best.",
  heroRatingVal: "5.0 / 5.0 Rating",
  heroRatingSub: "Over 1,200+ Radiant Clients",
  heroPromoBadgeTitle: "Signature 5+1 Sessions",
  heroPromoBadgeSub: "Buy 5 Get 1 Complimentary",
  feat1Title: "Certified Specialists",
  feat1Sub: "Board-trained aestheticians",
  feat2Title: "Medical Grade Tech",
  feat2Sub: "Diode Ice & Pico Lasers",
  feat3Title: "100% Authentic Drips",
  feat3Sub: "Japan & Korea Formulated",
  aboutSubtitle: "The Sanctuary Experience",
  aboutTitle: "Luxury Aesthetics Rooted in Care & Science",
  aboutYears: "5+",
  aboutYearsText: "Years of Aesthetic Excellence",
  aboutPara1: "Founded with a passion for transformative beauty, Roselie's Beauty Lounge provides an elevated salon and clinical aesthetics escape where relaxation and visible results coexist seamlessly.",
  aboutPara2: "From the moment you step through our doors, our dedicated team of licensed aestheticians and master stylists caters to your every comfort in private, meticulously sterilized suites designed for deep rejuvenation.",
  pillar1Title: "Hospital-Grade Hygiene",
  pillar1Desc: "Autoclaved instruments and single-use consumables for every client.",
  pillar2Title: "Authentic Formulations",
  pillar2Desc: "Directly imported Korean hydro-solutions and Japanese medical gluta drips.",
  pillar3Title: "Empathetic Consultation",
  pillar3Desc: "No rushed appointments. Every treatment is customized to your unique goals.",
  contactAddress: "Roselie's Beauty Lounge, Commercial Center",
  contactPhone: "+63 917 123 4567",
  contactLandline: "(02) 8123 4567",
  contactHours: "Monday to Sunday: 10:00 AM – 8:00 PM (Open on all regular holidays)",
  contactEmail: "concierge@roseliesbeautylounge.com",
  socialFb: "https://facebook.com",
  socialIg: "https://instagram.com",
  socialTiktok: "https://tiktok.com",
  socialWa: "https://wa.me/639171234567"
};

const DEFAULT_CMS_PRODUCTS = [
  { id: "prod-1", name: "Platinum Gluta-C Glow Capsules (60s)", price: 1250, priceDisplay: "₱1,250.00", tag: "Best Seller", icon: "fa-prescription-bottle-medical", desc: "Authentic Japan-grade 500mg L-Glutathione, Vitamin C, and collagen peptides for continuous systemic radiance." },
  { id: "prod-2", name: "Luminous Vita-C Brightening Serum (30ml)", price: 680, priceDisplay: "₱680.00", tag: "Clinical", icon: "fa-pump-soap", desc: "Potent 15% Ethyl Ascorbic Acid with ferulic acid to fade dark spots and boost skin luminosity." },
  { id: "prod-3", name: "Invisible Fluid Sunscreen SPF 50+ PA++++", price: 550, priceDisplay: "₱550.00", tag: "Essential", icon: "fa-sun", desc: "Ultra-lightweight hybrid sunscreen that leaves zero white cast and protects post-laser skin effortlessly." },
  { id: "prod-4", name: "Bio-Cellulose Hydra Recovery Mask (5 sheets)", price: 480, priceDisplay: "₱480.00", tag: "Spa Grade", icon: "fa-mask-face", desc: "Infused with 5 weights of Hyaluronic Acid and Centella Asiatica for immediate skin soothing and plumping." },
  { id: "prod-5", name: "Salon Intense Keratin Hair Mask (250g)", price: 650, priceDisplay: "₱650.00", tag: "Hair Studio", icon: "fa-wand-magic-sparkles", desc: "Restores chemically treated and rebonded hair bonds with hydrolyzed silk and argan oil." },
  { id: "prod-6", name: "Rosehip & Collagen Nourishing Cuticle Oil", price: 280, priceDisplay: "₱280.00", tag: "Nail Care", icon: "fa-hand-sparkles", desc: "Non-greasy dropper oil to hydrate dry cuticles and extend the life of your gel manicures." }
];

const DEFAULT_CMS_REVIEWS = [
  { id: "rev-1", name: "Bea P.", avatar: "BP", tag: "Regular Client • Japan Drip & Mermaid Facial", rating: 5, comment: "The Mermaid Facial and Japan Drip combo is life-changing! My skin was glowing for weeks. The lounge ambiance is so peaceful, clean, and luxurious. Definitely my holy grail beauty sanctuary!" },
  { id: "rev-2", name: "Katrina L.", avatar: "KL", tag: "Package Client • Diode Hair Removal 5+1", rating: 5, comment: "I availed of the Diode Hair Removal 5+1 package. By my 3rd session, hair growth was almost completely gone and painless! The nurse was so gentle and thorough. Super worth every peso!" },
  { id: "rev-3", name: "Maria G.", avatar: "MG", tag: "Salon Client • L'Oréal Rebond & Gel Overlay", rating: 5, comment: "Got my L'Oréal rebond and Gel nails done here. The stylists actually take care of your hair health without burning it. It's so silky smooth! Beautiful interior and courteous staff." }
];

const DEFAULT_CMS_FAQS = [
  { id: "faq-1", question: "How does the 5 + 1 Session Package work?", answer: "When you purchase any signature 5+1 package, you pay for 5 sessions and receive your 6th session completely free. Sessions can be scheduled whenever convenient over a 12-month period, with VIP priority booking slots." },
  { id: "faq-2", question: "Is the Diode Laser hair removal painful?", answer: "Our medical-grade Diode Ice Laser features active sapphire contact cooling (-5°C) that numbs the skin continuously throughout the pulse. Most clients describe it as feeling like a gentle cool glide with zero downtime." },
  { id: "faq-3", question: "How often can I receive a Gluta Drip or Push?", answer: "For initial skin radiance and antioxidant benefits, treatments are generally scheduled once a week or once every two weeks. Our registered nurses assess your hydration, blood pressure, and medical history before every drip." },
  { id: "faq-4", question: "What payment methods do you accept at the lounge?", answer: "We accept Cash (PHP), GCash, Maya, Bank Transfer (BDO, BPI, UnionBank), and all major Credit and Debit Cards (Visa, Mastercard, JCB)." },
  { id: "faq-5", question: "Do you accept walk-in clients?", answer: "Yes, walk-in clients are warmly welcomed! However, to avoid waiting times and secure your private treatment suite, we strongly recommend reserving your slot online via our booking engine." }
];

let appointments = getStorage('rbl_appointments', SEED_APPOINTMENTS);
let orders = getStorage('rbl_orders', SEED_ORDERS);
let messages = getStorage('rbl_messages', SEED_MESSAGES);
let catalog = getStorage('rbl_custom_catalog', DEFAULT_SERVICES);
let siteContent = getStorage('rbl_site_content', DEFAULT_SITE_CONTENT);
let testimonials = getStorage('rbl_testimonials', DEFAULT_CMS_REVIEWS);
let faqs = getStorage('rbl_faqs', DEFAULT_CMS_FAQS);
let retailProducts = getStorage('rbl_products', DEFAULT_CMS_PRODUCTS);

// Active Modal Context
let activeAppointmentId = null;
let activeOrderId = null;

// ==========================================================================
// 3. AUTHENTICATION CONTROLLER
// ==========================================================================

function checkAuth() {
  const isAuth = sessionStorage.getItem('rbl_admin_logged') === 'true' || localStorage.getItem('rbl_admin_logged') === 'true';
  const overlay = document.getElementById('adminAuthOverlay');
  if (isAuth) {
    overlay.style.display = 'none';
  } else {
    overlay.style.display = 'flex';
  }
}

const SUPER_ADMIN_DEFAULT_USER = 'admin';
const SUPER_ADMIN_DEFAULT_PASS = 'wesleyhans123';

function loginAdmin(user, pass) {
  const currentPass = localStorage.getItem('rbl_admin_password') || SUPER_ADMIN_DEFAULT_PASS;
  const currentUser = localStorage.getItem('rbl_admin_username') || SUPER_ADMIN_DEFAULT_USER;

  const rawUser = (user || '').trim().toLowerCase();
  const rawPass = (pass || '').trim();

  // Accept username 'admin', 'super admin', 'superadmin', or custom stored username
  const isUserValid = rawUser === currentUser.toLowerCase() ||
                      rawUser === SUPER_ADMIN_DEFAULT_USER.toLowerCase() ||
                      rawUser === 'super admin' ||
                      rawUser === 'superadmin';

  // Accept password matching stored password or default 'wesleyhans123'
  const isPassValid = rawPass === currentPass ||
                      pass === currentPass ||
                      rawPass === SUPER_ADMIN_DEFAULT_PASS ||
                      pass === SUPER_ADMIN_DEFAULT_PASS;

  const errorAlert = document.getElementById('authErrorAlert');
  const errorText = document.getElementById('authErrorText');

  if (isUserValid && isPassValid) {
    if (errorAlert) errorAlert.style.display = 'none';
    sessionStorage.setItem('rbl_admin_logged', 'true');
    localStorage.setItem('rbl_admin_logged', 'true');

    const overlay = document.getElementById('adminAuthOverlay');
    if (overlay) overlay.style.display = 'none';

    showAdminToast("Welcome back! Signed in to Roselie's Super Admin Console.", "success");
    try {
      refreshAllPanels();
    } catch (err) {
      console.warn("Panel refresh error:", err);
    }
    return true;
  } else {
    if (errorAlert && errorText) {
      errorText.textContent = "Invalid credentials. Use Username: admin, Password: wesleyhans123";
      errorAlert.style.display = 'block';
    }
    showAdminToast("Invalid username or password.", "danger");
    return false;
  }
}

function logoutAdmin() {
  sessionStorage.removeItem('rbl_admin_logged');
  localStorage.removeItem('rbl_admin_logged');
  document.getElementById('adminAuthOverlay').style.display = 'flex';
  showAdminToast("Signed out successfully.", "warning");
}

// ==========================================================================
// 4. TAB NAVIGATION
// ==========================================================================

function switchTab(tabId) {
  document.querySelectorAll('.nav-tab-btn').forEach(btn => {
    btn.classList.toggle('active', btn.dataset.tab === tabId);
  });

  document.querySelectorAll('.tab-pane').forEach(pane => {
    pane.classList.remove('active');
  });

  const target = document.getElementById(`pane-${tabId}`);
  if (target) {
    target.classList.add('active');
  }

  // Refresh data for the newly active tab
  if (tabId === 'overview') renderOverview();
  if (tabId === 'appointments') renderAppointments();
  if (tabId === 'orders') renderOrders();
  if (tabId === 'services') renderServices();
  if (tabId === 'messages') renderMessages();
  if (tabId === 'cms') renderCmsPanels();
}

// ==========================================================================
// 5. RENDERING MODULES
// ==========================================================================

function refreshAllPanels() {
  appointments = getStorage('rbl_appointments', SEED_APPOINTMENTS);
  orders = getStorage('rbl_orders', SEED_ORDERS);
  messages = getStorage('rbl_messages', SEED_MESSAGES);
  catalog = getStorage('rbl_custom_catalog', DEFAULT_SERVICES);
  siteContent = getStorage('rbl_site_content', DEFAULT_SITE_CONTENT);
  testimonials = getStorage('rbl_testimonials', DEFAULT_CMS_REVIEWS);
  faqs = getStorage('rbl_faqs', DEFAULT_CMS_FAQS);
  retailProducts = getStorage('rbl_products', DEFAULT_CMS_PRODUCTS);

  try { updateBadges(); } catch (e) { console.warn("updateBadges:", e); }
  try { renderOverview(); } catch (e) { console.warn("renderOverview:", e); }
  try { renderAppointments(); } catch (e) { console.warn("renderAppointments:", e); }
  try { renderServices(); } catch (e) { console.warn("renderServices:", e); }
  try { renderMessages(); } catch (e) { console.warn("renderMessages:", e); }
  try { renderCmsPanels(); } catch (e) { console.warn("renderCmsPanels:", e); }
  try { populateServiceSelects(); } catch (e) { console.warn("populateServiceSelects:", e); }
  try { populateSettingsFields(); } catch (e) { console.warn("populateSettingsFields:", e); }
}

function updateBadges() {
  const pendingApps = appointments.filter(a => a.status === 'Pending').length;
  const newMsgs = messages.filter(m => m.status === 'New').length;

  const badgePending = document.getElementById('badgePendingAppointments');
  if (badgePending) badgePending.textContent = pendingApps;
  const badgeNewOrds = document.getElementById('badgeNewOrders');
  if (badgeNewOrds) badgeNewOrds.textContent = 0;
  const badgeMsgs = document.getElementById('badgeNewMessages');
  if (badgeMsgs) badgeMsgs.textContent = newMsgs;

  const kpiTotal = document.getElementById('kpiTotalBookings');
  if (kpiTotal) kpiTotal.textContent = appointments.length;
  const kpiPending = document.getElementById('kpiPendingBookings');
  if (kpiPending) kpiPending.textContent = pendingApps;

  const kpiServices = document.getElementById('kpiActiveServices');
  if (kpiServices) kpiServices.textContent = (catalog && catalog.length) ? `${catalog.length}+` : '40+';

  const kpiBoutique = document.getElementById('kpiBoutiqueSales');
  if (kpiBoutique) {
    const totalRetailSales = orders.reduce((sum, o) => sum + (o.status !== 'Cancelled' ? o.total : 0), 0);
    kpiBoutique.textContent = `₱${totalRetailSales.toLocaleString()}.00`;
  }
  const kpiMsgs = document.getElementById('kpiTotalMessages');
  if (kpiMsgs) kpiMsgs.textContent = messages.length;
}

// --- OVERVIEW TAB ---
function renderOverview() {
  const tbodyBookings = document.getElementById('overviewBookingsBody');
  const tbodyOrders = document.getElementById('overviewOrdersBody');

  // Top 5 Bookings
  const recentBookings = [...appointments].reverse().slice(0, 5);
  if (tbodyBookings) {
    if (recentBookings.length === 0) {
      tbodyBookings.innerHTML = `<tr><td colspan="8" class="table-empty-state"><p>No appointments recorded yet.</p></td></tr>`;
    } else {
      tbodyBookings.innerHTML = recentBookings.map(a => `
        <tr>
          <td><strong>${a.id}</strong></td>
          <td>${a.clientName}</td>
          <td>${a.phone}</td>
          <td>${a.service}</td>
          <td>${a.schedule || a.date}</td>
          <td><strong class="text-rose">${a.priceDisplay || '₱' + a.price}</strong></td>
          <td><span class="badge badge-${a.status.toLowerCase()}">${a.status}</span></td>
          <td>
            <div class="row-actions">
              ${a.status === 'Pending' ? `
                <button class="btn-icon-action approve" onclick="updateAppointmentStatus('${a.id}', 'Confirmed')" title="Approve">
                  <i class="fa-solid fa-check"></i>
                </button>
              ` : ''}
              <button class="btn-icon-action" onclick="openAppointmentModal('${a.id}')" title="Details">
                <i class="fa-regular fa-eye"></i>
              </button>
            </div>
          </td>
        </tr>
      `).join('');
    }
  }

  // Top 5 Orders (if table present)
  if (tbodyOrders) {
    const recentOrders = [...orders].reverse().slice(0, 5);
    if (recentOrders.length === 0) {
      tbodyOrders.innerHTML = `<tr><td colspan="7" class="table-empty-state"><p>No boutique orders recorded yet.</p></td></tr>`;
    } else {
      tbodyOrders.innerHTML = recentOrders.map(o => `
        <tr>
          <td><strong>${o.id}</strong></td>
          <td>${o.customerName}</td>
          <td>${o.itemsSummary || (o.items ? o.items.length + ' items' : '-')}</td>
          <td><strong class="text-rose">${o.totalDisplay || '₱' + o.total}</strong></td>
          <td>${o.paymentMethod || 'Cash'}</td>
          <td><span class="badge badge-${getOrderBadgeClass(o.status)}">${o.status}</span></td>
          <td>
            <button class="btn-icon-action" onclick="openOrderModal('${o.id}')" title="View Order">
              <i class="fa-regular fa-eye"></i>
            </button>
          </td>
        </tr>
      `).join('');
    }
  }
}

// --- APPOINTMENTS TAB ---
function renderAppointments() {
  const tbody = document.getElementById('appointmentsTableBody');
  const search = document.getElementById('appointmentSearch')?.value.toLowerCase().trim() || '';
  const filter = document.getElementById('appointmentStatusFilter')?.value || 'all';

  let list = [...appointments].reverse();

  if (filter !== 'all') {
    list = list.filter(a => a.status === filter);
  }

  if (search) {
    list = list.filter(a => 
      a.id.toLowerCase().includes(search) ||
      a.clientName.toLowerCase().includes(search) ||
      a.phone.includes(search) ||
      a.service.toLowerCase().includes(search)
    );
  }

  document.getElementById('appointmentCountDisplay').textContent = `${list.length} bookings`;

  if (list.length === 0) {
    tbody.innerHTML = `
      <tr>
        <td colspan="9" class="table-empty-state">
          <i class="fa-regular fa-calendar-xmark"></i>
          <h4>No matching reservations found</h4>
          <p>Try clearing your search query or status filter.</p>
        </td>
      </tr>
    `;
    return;
  }

  tbody.innerHTML = list.map(a => `
    <tr>
      <td><strong>${a.id}</strong></td>
      <td><strong>${a.clientName}</strong></td>
      <td>${a.phone}</td>
      <td>${a.service}</td>
      <td>${a.schedule || `${a.date} at ${a.time}`}</td>
      <td>${a.specialist || 'Senior Aesthetician'}</td>
      <td><strong class="text-rose">${a.priceDisplay || '₱' + a.price}</strong></td>
      <td><span class="badge badge-${a.status.toLowerCase()}">${a.status}</span></td>
      <td>
        <div class="row-actions">
          ${a.status === 'Pending' ? `
            <button class="btn-icon-action approve" onclick="updateAppointmentStatus('${a.id}', 'Confirmed')" title="Confirm">
              <i class="fa-solid fa-check"></i>
            </button>
          ` : ''}
          ${a.status === 'Confirmed' ? `
            <button class="btn-icon-action complete" onclick="updateAppointmentStatus('${a.id}', 'Completed')" title="Complete">
              <i class="fa-solid fa-circle-check"></i>
            </button>
          ` : ''}
          <button class="btn-icon-action" onclick="openAppointmentModal('${a.id}')" title="View Full Details">
            <i class="fa-regular fa-eye"></i>
          </button>
          <button class="btn-icon-action delete" onclick="deleteAppointment('${a.id}')" title="Delete">
            <i class="fa-regular fa-trash-can"></i>
          </button>
        </div>
      </td>
    </tr>
  `).join('');
}

// --- BOUTIQUE ORDERS TAB ---
function renderOrders() {
  const tbody = document.getElementById('ordersTableBody');
  const countDisplay = document.getElementById('orderCountDisplay');
  if (!tbody || !countDisplay) return;

  const search = document.getElementById('orderSearch')?.value.toLowerCase().trim() || '';
  const filter = document.getElementById('orderStatusFilter')?.value || 'all';

  let list = [...orders].reverse();

  if (filter !== 'all') {
    list = list.filter(o => o.status === filter);
  }

  if (search) {
    list = list.filter(o => 
      o.id.toLowerCase().includes(search) ||
      o.customerName.toLowerCase().includes(search) ||
      o.phone.includes(search)
    );
  }

  countDisplay.textContent = `${list.length} orders`;

  if (list.length === 0) {
    tbody.innerHTML = `
      <tr>
        <td colspan="10" class="table-empty-state">
          <i class="fa-solid fa-bag-shopping"></i>
          <h4>No retail orders found</h4>
          <p>Orders submitted from the website boutique cart will appear here.</p>
        </td>
      </tr>
    `;
    return;
  }

  tbody.innerHTML = list.map(o => `
    <tr>
      <td><strong>${o.id}</strong></td>
      <td>${o.date || 'Today'}</td>
      <td><strong>${o.customerName}</strong></td>
      <td>${o.phone}</td>
      <td style="max-width:180px; font-size:0.8rem; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;" title="${o.address}">${o.address}</td>
      <td>${o.itemsSummary || (o.items ? o.items.length + ' items' : '-')}</td>
      <td><strong class="text-rose">${o.totalDisplay || '₱' + o.total}</strong></td>
      <td>${o.paymentMethod || 'GCash'}</td>
      <td><span class="badge badge-${getOrderBadgeClass(o.status)}">${o.status}</span></td>
      <td>
        <div class="row-actions">
          <button class="btn-icon-action" onclick="openOrderModal('${o.id}')" title="Order Details">
            <i class="fa-regular fa-eye"></i>
          </button>
          <button class="btn-icon-action delete" onclick="deleteOrder('${o.id}')" title="Delete">
            <i class="fa-regular fa-trash-can"></i>
          </button>
        </div>
      </td>
    </tr>
  `).join('');
}

function getOrderBadgeClass(status) {
  if (status === 'Completed') return 'completed';
  if (status === 'Processing') return 'confirmed';
  if (status === 'Ready for Pickup') return 'confirmed';
  if (status === 'Pending') return 'pending';
  return 'cancelled';
}

// --- SERVICES CATALOG TAB ---
function renderServices() {
  const tbody = document.getElementById('servicesTableBody');
  const search = document.getElementById('serviceSearch')?.value.toLowerCase().trim() || '';
  const catFilter = document.getElementById('serviceCategoryFilter')?.value || 'all';

  let list = [...catalog];

  if (catFilter !== 'all') {
    list = list.filter(s => s.category === catFilter);
  }

  if (search) {
    list = list.filter(s => s.name.toLowerCase().includes(search) || s.desc.toLowerCase().includes(search));
  }

  document.getElementById('serviceCountDisplay').textContent = `${list.length} treatments`;

  tbody.innerHTML = list.map(s => `
    <tr>
      <td><strong>${s.name}</strong></td>
      <td><span class="badge badge-active">${s.categoryLabel || s.category.toUpperCase()}</span></td>
      <td><strong class="text-rose">${s.priceDisplay || '₱' + s.price + '.00'}</strong></td>
      <td>${s.duration || '45 mins'}</td>
      <td>${s.packageAvailable ? `<strong style="color:#A85E67;">${s.packagePriceDisplay || '₱' + s.packagePrice + '.00'}</strong>` : '<span style="color:#A09BA5;">Single only</span>'}</td>
      <td style="max-width:260px; font-size:0.82rem; color:var(--muted-gray);">${s.desc}</td>
      <td>
        <div class="row-actions">
          <button class="btn-icon-action" onclick="openEditServiceModal('${s.id}')" title="Edit Price & Info">
            <i class="fa-regular fa-pen-to-square"></i>
          </button>
          <button class="btn-icon-action delete" onclick="deleteService('${s.id}')" title="Delete">
            <i class="fa-regular fa-trash-can"></i>
          </button>
        </div>
      </td>
    </tr>
  `).join('');
}

// --- MESSAGES TAB ---
function renderMessages() {
  const tbody = document.getElementById('messagesTableBody');
  const search = document.getElementById('messageSearch')?.value.toLowerCase().trim() || '';

  let list = [...messages].reverse();

  if (search) {
    list = list.filter(m => m.name.toLowerCase().includes(search) || m.phone.includes(search) || m.subject.toLowerCase().includes(search));
  }

  document.getElementById('messageCountDisplay').textContent = `${list.length} inquiries`;

  if (list.length === 0) {
    tbody.innerHTML = `<tr><td colspan="7" class="table-empty-state"><p>No inquiries received yet.</p></td></tr>`;
    return;
  }

  tbody.innerHTML = list.map(m => `
    <tr>
      <td>${m.date || 'Today'}</td>
      <td><strong>${m.name}</strong></td>
      <td>${m.phone}</td>
      <td><strong>${m.subject}</strong></td>
      <td style="max-width:300px; font-size:0.83rem;">${m.message}</td>
      <td><span class="badge badge-${m.status === 'New' ? 'pending' : 'completed'}">${m.status}</span></td>
      <td>
        <div class="row-actions">
          <a href="https://wa.me/${m.phone.replace(/[^0-9]/g, '')}" target="_blank" class="btn-icon-action" title="Chat on WhatsApp">
            <i class="fa-brands fa-whatsapp text-rose"></i>
          </a>
          <button class="btn-icon-action complete" onclick="toggleMessageStatus('${m.id}')" title="Toggle Status">
            <i class="fa-solid fa-check"></i>
          </button>
          <button class="btn-icon-action delete" onclick="deleteMessage('${m.id}')" title="Delete">
            <i class="fa-regular fa-trash-can"></i>
          </button>
        </div>
      </td>
    </tr>
  `).join('');
}

// ==========================================================================
// 6. APPOINTMENT ACTIONS & MODALS
// ==========================================================================

function openAppointmentModal(appId) {
  const a = appointments.find(x => x.id === appId);
  if (!a) return;

  activeAppointmentId = appId;
  document.getElementById('madCode').textContent = a.id;
  document.getElementById('madName').textContent = a.clientName;
  document.getElementById('madPhone').textContent = a.phone;
  document.getElementById('madEmail').textContent = a.email || 'Not provided';
  document.getElementById('madService').textContent = a.service;
  document.getElementById('madSchedule').textContent = a.schedule || `${a.date} at ${a.time}`;
  document.getElementById('madSpecialist').textContent = a.specialist || 'Senior Aesthetician';
  document.getElementById('madPrice').textContent = a.priceDisplay || `₱${a.price}.00`;
  document.getElementById('madNotes').textContent = a.notes || 'No special requests provided.';

  const badge = document.getElementById('madStatusBadge');
  badge.className = `badge badge-${a.status.toLowerCase()}`;
  badge.textContent = a.status;

  document.getElementById('madStatusSelect').value = a.status;

  // WhatsApp click link
  const waPhone = a.phone.replace(/[^0-9]/g, '');
  const waMsg = encodeURIComponent(`Hello ${a.clientName}! This is Roselie's Beauty Lounge regarding your reservation (${a.id}) for ${a.service} scheduled on ${a.schedule || a.date}. Your status is currently: ${a.status}.`);
  document.getElementById('madWhatsAppBtn').href = `https://wa.me/${waPhone}?text=${waMsg}`;

  document.getElementById('modalAppointmentDetail').classList.add('active');
}

function updateAppointmentStatus(appId, newStatus) {
  const a = appointments.find(x => x.id === appId);
  if (!a) return;

  a.status = newStatus;
  setStorage('rbl_appointments', appointments);
  refreshAllPanels();
  showAdminToast(`Reservation ${appId} updated to "${newStatus}"!`, "success");
}

function deleteAppointment(appId) {
  if (!confirm(`Are you sure you want to delete reservation ${appId}?`)) return;
  appointments = appointments.filter(a => a.id !== appId);
  setStorage('rbl_appointments', appointments);
  refreshAllPanels();
  showAdminToast(`Reservation ${appId} deleted.`, "warning");
}

document.getElementById('madSaveStatusBtn')?.addEventListener('click', () => {
  if (!activeAppointmentId) return;
  const newStatus = document.getElementById('madStatusSelect').value;
  updateAppointmentStatus(activeAppointmentId, newStatus);
  closeModal('modalAppointmentDetail');
});

// Create Manual Reservation
document.getElementById('btnOpenNewBookingModal')?.addEventListener('click', () => {
  document.getElementById('newAppointmentForm').reset();
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  const pad = (n) => String(n).padStart(2, '0');
  document.getElementById('naDate').value = `${tomorrow.getFullYear()}-${pad(tomorrow.getMonth() + 1)}-${pad(tomorrow.getDate())}`;
  document.getElementById('modalNewAppointment').classList.add('active');
});

document.getElementById('btnAddNewAppointment2')?.addEventListener('click', () => {
  document.getElementById('btnOpenNewBookingModal').click();
});

document.getElementById('newAppointmentForm')?.addEventListener('submit', (e) => {
  e.preventDefault();
  const fName = document.getElementById('naFirstName').value.trim();
  const lName = document.getElementById('naLastName').value.trim();
  const phone = document.getElementById('naPhone').value.trim();
  const email = document.getElementById('naEmail').value.trim();
  const srvId = document.getElementById('naServiceSelect').value;
  const srvObj = catalog.find(s => s.id === srvId) || { name: "Custom Service", price: 500, priceDisplay: "₱500.00" };
  const dateVal = document.getElementById('naDate').value;
  const timeVal = document.getElementById('naTime').value;
  const specialist = document.getElementById('naSpecialist').value;
  const status = document.getElementById('naStatus').value;
  const notes = document.getElementById('naNotes').value.trim();

  const code = 'RBL-' + Math.floor(1000 + Math.random() * 9000);
  const dateObj = new Date(dateVal);
  const dateFormatted = dateObj.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' });

  const newApp = {
    id: code,
    clientName: `${fName} ${lName}`,
    firstName: fName,
    lastName: lName,
    phone: phone,
    email: email,
    service: srvObj.name,
    date: dateVal,
    time: timeVal,
    schedule: `${dateFormatted} at ${timeVal}`,
    specialist: specialist,
    price: srvObj.price,
    priceDisplay: srvObj.priceDisplay,
    status: status,
    notes: notes,
    createdDate: new Date().toISOString()
  };

  appointments.push(newApp);
  setStorage('rbl_appointments', appointments);
  refreshAllPanels();
  closeModal('modalNewAppointment');
  showAdminToast(`New reservation ${code} created successfully!`, "success");
});

// ==========================================================================
// 7. ORDER ACTIONS & MODALS
// ==========================================================================

function openOrderModal(orderId) {
  const o = orders.find(x => x.id === orderId);
  if (!o) return;

  activeOrderId = orderId;
  document.getElementById('modCode').textContent = o.id;
  document.getElementById('modName').textContent = o.customerName;
  document.getElementById('modPhone').textContent = o.phone;
  document.getElementById('modAddress').textContent = o.address;
  document.getElementById('modPayment').textContent = o.paymentMethod || 'GCash';
  document.getElementById('modTotal').textContent = o.totalDisplay || `₱${o.total}.00`;
  document.getElementById('modStatusSelect').value = o.status;

  const itemsList = document.getElementById('modItemsList');
  if (o.items && o.items.length > 0) {
    itemsList.innerHTML = o.items.map(it => `
      <div style="display:flex; justify-content:space-between; margin-bottom:4px;">
        <span>${it.qty}x ${it.name}</span>
        <strong>₱${(it.price * it.qty).toLocaleString()}.00</strong>
      </div>
    `).join('');
  } else {
    itemsList.innerHTML = `<div>${o.itemsSummary || 'Standard retail package'}</div>`;
  }

  document.getElementById('modalOrderDetail').classList.add('active');
}

document.getElementById('modSaveStatusBtn')?.addEventListener('click', () => {
  if (!activeOrderId) return;
  const o = orders.find(x => x.id === activeOrderId);
  if (!o) return;

  const newStatus = document.getElementById('modStatusSelect').value;
  o.status = newStatus;
  setStorage('rbl_orders', orders);
  refreshAllPanels();
  closeModal('modalOrderDetail');
  showAdminToast(`Order ${activeOrderId} status set to "${newStatus}"!`, "success");
});

function deleteOrder(orderId) {
  if (!confirm(`Are you sure you want to delete order ${orderId}?`)) return;
  orders = orders.filter(o => o.id !== orderId);
  setStorage('rbl_orders', orders);
  refreshAllPanels();
  showAdminToast(`Order ${orderId} deleted.`, "warning");
}

// ==========================================================================
// 8. SERVICES CATALOG ACTIONS & MODALS
// ==========================================================================

function populateServiceSelects() {
  const select = document.getElementById('naServiceSelect');
  if (!select) return;

  select.innerHTML = catalog.map(s => `
    <option value="${s.id}">${s.name} (${s.priceDisplay || '₱' + s.price})</option>
  `).join('');
}

function openEditServiceModal(serviceId) {
  const s = catalog.find(x => x.id === serviceId);
  if (!s) return;

  document.getElementById('msfTitle').textContent = `Edit Service: ${s.name}`;
  document.getElementById('msfId').value = s.id;
  document.getElementById('msfName').value = s.name;
  document.getElementById('msfCategory').value = s.category;
  document.getElementById('msfPrice').value = s.price;
  document.getElementById('msfDuration').value = s.duration || '45 mins';
  document.getElementById('msfPackageAvailable').value = s.packageAvailable ? 'true' : 'false';
  document.getElementById('msfPackagePrice').value = s.packagePrice || (s.price * 5);
  document.getElementById('msfDesc').value = s.desc;

  document.getElementById('modalServiceForm').classList.add('active');
}

document.getElementById('btnAddNewService')?.addEventListener('click', () => {
  document.getElementById('msfTitle').textContent = "Add New Treatment Service";
  document.getElementById('serviceEditForm').reset();
  document.getElementById('msfId').value = "";
  document.getElementById('modalServiceForm').classList.add('active');
});

document.getElementById('serviceEditForm')?.addEventListener('submit', (e) => {
  e.preventDefault();
  const id = document.getElementById('msfId').value;
  const name = document.getElementById('msfName').value.trim();
  const cat = document.getElementById('msfCategory').value;
  const price = parseFloat(document.getElementById('msfPrice').value);
  const duration = document.getElementById('msfDuration').value.trim();
  const pkgAvail = document.getElementById('msfPackageAvailable').value === 'true';
  const pkgPrice = parseFloat(document.getElementById('msfPackagePrice').value) || (price * 5);
  const desc = document.getElementById('msfDesc').value.trim();

  const catLabels = {
    gluta: "Gluta Drips & Push",
    facial: "Clinical Facial",
    laser: "Laser Clinic",
    hair: "Hair Studio",
    nails: "Nail Spa"
  };

  if (id) {
    // Update existing
    const s = catalog.find(x => x.id === id);
    if (s) {
      s.name = name;
      s.category = cat;
      s.categoryLabel = catLabels[cat] || "Treatment";
      s.price = price;
      s.priceDisplay = `₱${price.toLocaleString()}.00`;
      s.duration = duration;
      s.packageAvailable = pkgAvail;
      s.packagePrice = pkgPrice;
      s.packagePriceDisplay = `₱${pkgPrice.toLocaleString()}.00`;
      s.desc = desc;
      showAdminToast(`Updated treatment "${name}"!`, "success");
    }
  } else {
    // Create new
    const newId = `srv-${cat}-${Date.now().toString().slice(-4)}`;
    const newSrv = {
      id: newId,
      name: name,
      category: cat,
      categoryLabel: catLabels[cat] || "Treatment",
      price: price,
      priceDisplay: `₱${price.toLocaleString()}.00`,
      duration: duration,
      packageAvailable: pkgAvail,
      packagePrice: pkgPrice,
      packagePriceDisplay: `₱${pkgPrice.toLocaleString()}.00`,
      desc: desc
    };
    catalog.push(newSrv);
    showAdminToast(`Added new treatment "${name}" to catalog!`, "success");
  }

  setStorage('rbl_custom_catalog', catalog);
  saveAutoSnapshot(`Updated catalog item: ${name}`);
  refreshAllPanels();
  closeModal('modalServiceForm');
});

function deleteService(serviceId) {
  const s = catalog.find(x => x.id === serviceId);
  if (!s) return;
  if (!confirm(`Are you sure you want to remove "${s.name}" from the catalog?`)) return;

  saveAutoSnapshot(`Pre-Delete Treatment: ${s.name}`);
  catalog = catalog.filter(x => x.id !== serviceId);
  setStorage('rbl_custom_catalog', catalog);
  refreshAllPanels();
  showAdminToast(`Removed "${s.name}" from catalog. (Safety snapshot saved)`, "warning");
}

document.getElementById('btnResetCatalog')?.addEventListener('click', () => {
  if (!confirm("Reset treatment catalog and pricing back to initial official salon defaults?\n(A safety backup snapshot will be saved so you can restore anytime).")) return;
  saveAutoSnapshot("Pre-Catalog Reset Safety Point");
  catalog = [...DEFAULT_SERVICES];
  setStorage('rbl_custom_catalog', catalog);
  refreshAllPanels();
  showAdminToast("Catalog reset to salon defaults. (Safety snapshot saved)", "warning");
});

// ==========================================================================
// 9. MESSAGES ACTIONS
// ==========================================================================

function toggleMessageStatus(msgId) {
  const m = messages.find(x => x.id === msgId);
  if (!m) return;
  m.status = m.status === 'New' ? 'Contacted' : 'New';
  setStorage('rbl_messages', messages);
  refreshAllPanels();
  showAdminToast(`Inquiry status updated to ${m.status}.`, "success");
}

function deleteMessage(msgId) {
  if (!confirm("Delete this client inquiry?")) return;
  messages = messages.filter(x => x.id !== msgId);
  setStorage('rbl_messages', messages);
  refreshAllPanels();
  showAdminToast("Inquiry deleted.", "warning");
}

// ==========================================================================
// 10. EXPORT & DATA UTILITIES
// ==========================================================================

function exportDataToCsv(type) {
  let headers = [];
  let rows = [];
  let filename = `Roselies-${type}-${new Date().toISOString().slice(0, 10)}.csv`;

  if (type === 'appointments') {
    headers = ["Voucher Code", "Client Name", "Phone", "Email", "Service", "Schedule", "Specialist", "Price (PHP)", "Status", "Client Notes"];
    rows = appointments.map(a => [
      `"${a.id}"`,
      `"${a.clientName.replace(/"/g, '""')}"`,
      `"${a.phone}"`,
      `"${a.email || ''}"`,
      `"${a.service.replace(/"/g, '""')}"`,
      `"${a.schedule || a.date}"`,
      `"${a.specialist || ''}"`,
      a.price,
      `"${a.status}"`,
      `"${(a.notes || '').replace(/"/g, '""')}"`
    ]);
  } else if (type === 'orders') {
    headers = ["Order ID", "Date", "Customer Name", "Phone", "Delivery Address", "Items Summary", "Total (PHP)", "Payment Method", "Status"];
    rows = orders.map(o => [
      `"${o.id}"`,
      `"${o.date || ''}"`,
      `"${o.customerName.replace(/"/g, '""')}"`,
      `"${o.phone}"`,
      `"${o.address.replace(/"/g, '""')}"`,
      `"${(o.itemsSummary || '').replace(/"/g, '""')}"`,
      o.total,
      `"${o.paymentMethod || ''}"`,
      `"${o.status}"`
    ]);
  } else if (type === 'messages') {
    headers = ["ID", "Date", "Client Name", "Phone", "Subject", "Message", "Status"];
    rows = messages.map(m => [
      `"${m.id}"`,
      `"${m.date || ''}"`,
      `"${m.name.replace(/"/g, '""')}"`,
      `"${m.phone}"`,
      `"${m.subject.replace(/"/g, '""')}"`,
      `"${m.message.replace(/"/g, '""')}"`,
      `"${m.status}"`
    ]);
  }

  const csvContent = "\uFEFF" + [headers.join(','), ...rows.map(r => r.join(','))].join('\r\n');
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);

  showAdminToast(`Exported ${rows.length} ${type} records to CSV!`, "success");
}

function createSystemSnapshot() {
  return {
    version: "2.1.0",
    exportedAt: new Date().toISOString(),
    snapshotDateFormatted: new Date().toLocaleString(),
    lounge: "Roselie's Beauty Lounge",
    siteContent: getStorage('rbl_site_content', DEFAULT_SITE_CONTENT),
    catalog: getStorage('rbl_custom_catalog', DEFAULT_SERVICES),
    appointments: getStorage('rbl_appointments', SEED_APPOINTMENTS),
    orders: getStorage('rbl_orders', SEED_ORDERS),
    messages: getStorage('rbl_messages', SEED_MESSAGES),
    testimonials: getStorage('rbl_testimonials', DEFAULT_CMS_REVIEWS),
    faqs: getStorage('rbl_faqs', DEFAULT_CMS_FAQS),
    retailProducts: getStorage('rbl_products', DEFAULT_CMS_PRODUCTS),
    announcement: localStorage.getItem('rbl_announcement') || "PROMO: Book any 5 sessions & get your 6th session FREE on all signature laser & gluta treatments!",
    phone: localStorage.getItem('rbl_phone') || "+63 917 123 4567"
  };
}

function saveAutoSnapshot(reason = "Auto-save") {
  try {
    const snapshot = createSystemSnapshot();
    snapshot.reason = reason;
    localStorage.setItem('rbl_auto_snapshot', JSON.stringify(snapshot));
    localStorage.setItem('rbl_auto_snapshot_time', new Date().toLocaleString());
  } catch (e) {
    console.warn("Auto snapshot error:", e);
  }
}

function exportFullJsonBackup() {
  const fullData = createSystemSnapshot();
  const blob = new Blob([JSON.stringify(fullData, null, 2)], { type: 'application/json' });
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = `Roselies-Backup-${new Date().toISOString().slice(0, 10)}.json`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(link.href);

  showAdminToast("Full site backup (.JSON) downloaded! Location, catalog & settings are safely exported.", "success");
}

function restoreAutoSnapshot() {
  try {
    const raw = localStorage.getItem('rbl_auto_snapshot');
    if (!raw) {
      showAdminToast("No auto-saved snapshot found in browser storage.", "warning");
      return;
    }
    const data = JSON.parse(raw);
    const snapTime = localStorage.getItem('rbl_auto_snapshot_time') || data.snapshotDateFormatted || 'recent session';
    if (!confirm(`Restore all lounge data, custom location & catalog from auto-saved snapshot (${snapTime})?`)) return;
    applyImportedBackupData(data);
    showAdminToast(`Successfully restored snapshot from ${snapTime}!`, "success");
  } catch (err) {
    showAdminToast("Error restoring snapshot: " + err.message, "danger");
  }
}

function handleBackupFileSelect(e) {
  const file = e.target.files && e.target.files[0];
  if (!file) return;
  const reader = new FileReader();
  reader.onload = function(evt) {
    try {
      const data = JSON.parse(evt.target.result);
      if (!data || typeof data !== 'object') {
        throw new Error("The selected file is not a valid JSON backup.");
      }
      if (!data.siteContent && !data.catalog && !data.appointments) {
        throw new Error("File does not contain valid Roselie's Beauty Lounge backup data.");
      }
      saveAutoSnapshot("Pre-Restore Safety Point");
      applyImportedBackupData(data);
      showAdminToast("Backup file restored successfully! Location, catalog & CMS content are live.", "success");
    } catch (err) {
      showAdminToast("Failed to restore backup: " + err.message, "danger");
    } finally {
      e.target.value = '';
    }
  };
  reader.readAsText(file);
}

function applyImportedBackupData(data) {
  if (data.siteContent && typeof data.siteContent === 'object') {
    const merged = { ...DEFAULT_SITE_CONTENT, ...data.siteContent };
    setStorage('rbl_site_content', merged);
    siteContent = merged;
  }
  if (Array.isArray(data.catalog)) {
    setStorage('rbl_custom_catalog', data.catalog);
    catalog = data.catalog;
  }
  if (Array.isArray(data.appointments)) {
    setStorage('rbl_appointments', data.appointments);
    appointments = data.appointments;
  }
  if (Array.isArray(data.orders)) {
    setStorage('rbl_orders', data.orders);
    orders = data.orders;
  }
  if (Array.isArray(data.messages)) {
    setStorage('rbl_messages', data.messages);
    messages = data.messages;
  }
  if (Array.isArray(data.testimonials)) {
    setStorage('rbl_testimonials', data.testimonials);
    testimonials = data.testimonials;
  }
  if (Array.isArray(data.faqs)) {
    setStorage('rbl_faqs', data.faqs);
    faqs = data.faqs;
  }
  if (Array.isArray(data.retailProducts)) {
    setStorage('rbl_products', data.retailProducts);
    retailProducts = data.retailProducts;
  }
  if (data.announcement) {
    localStorage.setItem('rbl_announcement', data.announcement);
    const annInput = document.getElementById('announcementInput');
    if (annInput) annInput.value = data.announcement;
  }
  if (data.phone) {
    localStorage.setItem('rbl_phone', data.phone);
    const phInput = document.getElementById('phoneInput');
    if (phInput) phInput.value = data.phone;
  }

  // Refresh all UI panels and fields immediately
  refreshAllPanels();
  populateCmsFields();
  populateSettingsFields();
}

function populateSettingsFields() {
  const savedAnn = localStorage.getItem('rbl_announcement');
  if (savedAnn) {
    const el = document.getElementById('announcementInput');
    if (el) el.value = savedAnn;
  }
  const savedPhone = localStorage.getItem('rbl_phone') || siteContent?.contactPhone;
  if (savedPhone) {
    const el = document.getElementById('phoneInput');
    if (el) el.value = savedPhone;
  }
}

document.getElementById('btnLoadDemoData')?.addEventListener('click', () => {
  if (!confirm("Load realistic demo appointments, inquiries, and orders?\n(A safety backup snapshot of your current data will be saved).")) return;
  saveAutoSnapshot("Pre-Demo Data Load");
  appointments = [...SEED_APPOINTMENTS];
  orders = [...SEED_ORDERS];
  messages = [...SEED_MESSAGES];
  // Do NOT wipe custom catalog or location!
  setStorage('rbl_appointments', appointments);
  setStorage('rbl_orders', orders);
  setStorage('rbl_messages', messages);

  refreshAllPanels();
  showAdminToast("Realistic demo records loaded into appointments & messages! (Custom catalog preserved)", "success");
});

document.getElementById('btnClearAllData')?.addEventListener('click', () => {
  if (!confirm("Are you sure? This will remove all local bookings, orders, and inquiries from your browser storage.\n(A safety backup snapshot will be saved so you can restore anytime).")) return;
  saveAutoSnapshot("Pre-Clear Records");
  appointments = [];
  orders = [];
  messages = [];
  setStorage('rbl_appointments', appointments);
  setStorage('rbl_orders', orders);
  setStorage('rbl_messages', messages);
  refreshAllPanels();
  showAdminToast("Database records cleared. (Safety snapshot saved)", "warning");
});

document.getElementById('announcementForm')?.addEventListener('submit', (e) => {
  e.preventDefault();
  const text = document.getElementById('announcementInput').value.trim();
  const phone = document.getElementById('phoneInput').value.trim();
  localStorage.setItem('rbl_announcement', text);
  localStorage.setItem('rbl_phone', phone);
  saveAutoSnapshot("Updated announcement banner");
  showAdminToast("Website banner & contact saved! Refresh public site to see changes.", "success");
});

// ==========================================================================
// 11. GENERAL UTILITIES (MODALS, TOASTS, EVENTS)
// ==========================================================================

function closeModal(modalId) {
  document.getElementById(modalId)?.classList.remove('active');
}

function showAdminToast(message, type = 'success') {
  const container = document.getElementById('adminToastContainer');
  if (!container) return;

  const toast = document.createElement('div');
  toast.className = `admin-toast ${type}`;
  
  let icon = 'fa-check';
  if (type === 'warning') icon = 'fa-triangle-exclamation';
  if (type === 'danger') icon = 'fa-circle-xmark';

  toast.innerHTML = `<i class="fa-solid ${icon}"></i><span>${message}</span>`;
  container.appendChild(toast);

  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transform = 'translateX(100%)';
    toast.style.transition = 'all 0.3s ease';
    setTimeout(() => toast.remove(), 300);
  }, 3500);
}

// ==========================================================================
// 12. CMS CONTENT MANAGEMENT SYSTEM CONTROLLER
// ==========================================================================

function escapeHtml(str) {
  if (str === null || str === undefined) return '';
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

function setVal(id, val) {
  const el = document.getElementById(id);
  if (el && val !== undefined && val !== null) el.value = val;
}

function getVal(id, fallback = '') {
  const el = document.getElementById(id);
  return el ? el.value.trim() : fallback;
}

function renderCmsPanels() {
  populateCmsFields();
  renderCmsReviews();
  renderCmsFaqs();
  renderCmsProducts();
}

function populateCmsFields() {
  siteContent = getStorage('rbl_site_content', DEFAULT_SITE_CONTENT);

  // 1. Hero & Highlights
  setVal('cmsHeroTag', siteContent.heroTag);
  setVal('cmsHeroTitle', siteContent.heroTitle);
  setVal('cmsHeroDesc', siteContent.heroDesc);
  setVal('cmsHeroRatingVal', siteContent.heroRatingVal);
  setVal('cmsHeroRatingSub', siteContent.heroRatingSub);
  setVal('cmsHeroPromoBadgeTitle', siteContent.heroPromoBadgeTitle);
  setVal('cmsHeroPromoBadgeSub', siteContent.heroPromoBadgeSub);
  setVal('cmsFeat1Title', siteContent.feat1Title);
  setVal('cmsFeat1Sub', siteContent.feat1Sub);
  setVal('cmsFeat2Title', siteContent.feat2Title);
  setVal('cmsFeat2Sub', siteContent.feat2Sub);
  setVal('cmsFeat3Title', siteContent.feat3Title);
  setVal('cmsFeat3Sub', siteContent.feat3Sub);

  // 2. About & Sanctuary Experience
  setVal('cmsAboutSubtitle', siteContent.aboutSubtitle);
  setVal('cmsAboutTitle', siteContent.aboutTitle);
  setVal('cmsAboutYears', siteContent.aboutYears);
  setVal('cmsAboutYearsText', siteContent.aboutYearsText);
  setVal('cmsAboutPara1', siteContent.aboutPara1);
  setVal('cmsAboutPara2', siteContent.aboutPara2);
  setVal('cmsPillar1Title', siteContent.pillar1Title);
  setVal('cmsPillar1Desc', siteContent.pillar1Desc);
  setVal('cmsPillar2Title', siteContent.pillar2Title);
  setVal('cmsPillar2Desc', siteContent.pillar2Desc);
  setVal('cmsPillar3Title', siteContent.pillar3Title);
  setVal('cmsPillar3Desc', siteContent.pillar3Desc);

  // 6. Contact, Hours & Socials
  setVal('cmsContactAddress', siteContent.contactAddress);
  setVal('cmsContactPhone', siteContent.contactPhone);
  setVal('cmsContactLandline', siteContent.contactLandline);
  setVal('cmsContactEmail', siteContent.contactEmail);
  setVal('cmsContactHours', siteContent.contactHours);
  setVal('cmsSocialFb', siteContent.socialFb);
  setVal('cmsSocialIg', siteContent.socialIg);
  setVal('cmsSocialTiktok', siteContent.socialTiktok);
  setVal('cmsSocialWa', siteContent.socialWa);
}

// --- CMS Reviews Manager ---
function renderCmsReviews() {
  const tbody = document.getElementById('cmsReviewsTableBody');
  if (!tbody) return;

  testimonials = getStorage('rbl_testimonials', DEFAULT_CMS_REVIEWS);

  if (!testimonials || testimonials.length === 0) {
    tbody.innerHTML = `<tr><td colspan="5" class="table-empty-state"><p>No testimonials added yet. Click "Add New Review" above.</p></td></tr>`;
    return;
  }

  tbody.innerHTML = testimonials.map(r => {
    const starIcons = '★'.repeat(r.rating || 5) + '☆'.repeat(Math.max(0, 5 - (r.rating || 5)));
    const excerpt = (r.comment || '').length > 80 ? (r.comment.slice(0, 80) + '...') : (r.comment || '');
    return `
      <tr>
        <td>
          <div class="user-cell">
            <div class="avatar-sm">${escapeHtml(r.avatar || (r.name ? r.name.slice(0, 2).toUpperCase() : 'CL'))}</div>
            <strong>${escapeHtml(r.name)}</strong>
          </div>
        </td>
        <td><span class="badge badge-package">${escapeHtml(r.tag)}</span></td>
        <td style="color:#D4AF37; font-weight:700; letter-spacing:2px;">${starIcons}</td>
        <td style="max-width:260px; font-size:0.85rem; color:var(--text-secondary);">${escapeHtml(excerpt)}</td>
        <td>
          <div class="table-actions">
            <button class="btn-table-action" onclick="openEditCmsReview('${r.id}')" title="Edit Review">
              <i class="fa-solid fa-pen-to-square"></i>
            </button>
            <button class="btn-table-action text-danger" onclick="deleteCmsReview('${r.id}')" title="Delete Review">
              <i class="fa-solid fa-trash-can"></i>
            </button>
          </div>
        </td>
      </tr>
    `;
  }).join('');
}

function openEditCmsReview(id) {
  testimonials = getStorage('rbl_testimonials', DEFAULT_CMS_REVIEWS);
  const rev = testimonials.find(r => r.id === id);
  if (!rev) return;

  document.getElementById('mcrId').value = rev.id;
  document.getElementById('mcrTitle').textContent = "Edit Client Testimonial";
  document.getElementById('mcrName').value = rev.name;
  document.getElementById('mcrRating').value = rev.rating || 5;
  document.getElementById('mcrTag').value = rev.tag || '';
  document.getElementById('mcrComment').value = rev.comment || '';

  document.getElementById('modalCmsReview')?.classList.add('active');
}

function deleteCmsReview(id) {
  if (!confirm("Are you sure you want to remove this client review from the website?")) return;
  testimonials = getStorage('rbl_testimonials', DEFAULT_CMS_REVIEWS);
  testimonials = testimonials.filter(r => r.id !== id);
  setStorage('rbl_testimonials', testimonials);
  renderCmsReviews();
  showAdminToast("Client review deleted! Website updated.", "warning");
}

// --- CMS FAQs Manager ---
function renderCmsFaqs() {
  const tbody = document.getElementById('cmsFaqsTableBody');
  if (!tbody) return;

  faqs = getStorage('rbl_faqs', DEFAULT_CMS_FAQS);

  if (!faqs || faqs.length === 0) {
    tbody.innerHTML = `<tr><td colspan="3" class="table-empty-state"><p>No FAQs configured. Click "Add New FAQ" above.</p></td></tr>`;
    return;
  }

  tbody.innerHTML = faqs.map(f => {
    const ansExcerpt = (f.answer || '').length > 90 ? (f.answer.slice(0, 90) + '...') : (f.answer || '');
    return `
      <tr>
        <td style="font-weight:600; color:var(--text-primary); max-width:280px;">${escapeHtml(f.question)}</td>
        <td style="max-width:350px; font-size:0.85rem; color:var(--text-secondary);">${escapeHtml(ansExcerpt)}</td>
        <td>
          <div class="table-actions">
            <button class="btn-table-action" onclick="openEditCmsFaq('${f.id}')" title="Edit FAQ">
              <i class="fa-solid fa-pen-to-square"></i>
            </button>
            <button class="btn-table-action text-danger" onclick="deleteCmsFaq('${f.id}')" title="Delete FAQ">
              <i class="fa-solid fa-trash-can"></i>
            </button>
          </div>
        </td>
      </tr>
    `;
  }).join('');
}

function openEditCmsFaq(id) {
  faqs = getStorage('rbl_faqs', DEFAULT_CMS_FAQS);
  const faq = faqs.find(f => f.id === id);
  if (!faq) return;

  document.getElementById('mcfId').value = faq.id;
  document.getElementById('mcfTitle').textContent = "Edit FAQ Item";
  document.getElementById('mcfQuestion').value = faq.question;
  document.getElementById('mcfAnswer').value = faq.answer;

  document.getElementById('modalCmsFaq')?.classList.add('active');
}

function deleteCmsFaq(id) {
  if (!confirm("Are you sure you want to delete this FAQ item?")) return;
  faqs = getStorage('rbl_faqs', DEFAULT_CMS_FAQS);
  faqs = faqs.filter(f => f.id !== id);
  setStorage('rbl_faqs', faqs);
  renderCmsFaqs();
  showAdminToast("FAQ removed from website.", "warning");
}

// --- CMS Retail Products Manager ---
function renderCmsProducts() {
  const tbody = document.getElementById('cmsProductsTableBody');
  if (!tbody) return;

  retailProducts = getStorage('rbl_products', DEFAULT_CMS_PRODUCTS);

  if (!retailProducts || retailProducts.length === 0) {
    tbody.innerHTML = `<tr><td colspan="5" class="table-empty-state"><p>No retail products found. Click "Add Retail Product" above.</p></td></tr>`;
    return;
  }

  tbody.innerHTML = retailProducts.map(p => {
    const descExcerpt = (p.desc || '').length > 70 ? (p.desc.slice(0, 70) + '...') : (p.desc || '');
    return `
      <tr>
        <td><strong>${escapeHtml(p.name)}</strong></td>
        <td><span class="badge badge-confirmed">${escapeHtml(p.tag || 'Boutique')}</span></td>
        <td style="font-weight:700; color:var(--dusty-rose);">₱${Number(p.price || 0).toLocaleString()}.00</td>
        <td style="max-width:240px; font-size:0.85rem; color:var(--text-secondary);">${escapeHtml(descExcerpt)}</td>
        <td>
          <div class="table-actions">
            <button class="btn-table-action" onclick="openEditCmsProduct('${p.id}')" title="Edit Product">
              <i class="fa-solid fa-pen-to-square"></i>
            </button>
            <button class="btn-table-action text-danger" onclick="deleteCmsProduct('${p.id}')" title="Delete Product">
              <i class="fa-solid fa-trash-can"></i>
            </button>
          </div>
        </td>
      </tr>
    `;
  }).join('');
}

function openEditCmsProduct(id) {
  retailProducts = getStorage('rbl_products', DEFAULT_CMS_PRODUCTS);
  const prod = retailProducts.find(p => p.id === id);
  if (!prod) return;

  document.getElementById('mcpId').value = prod.id;
  document.getElementById('mcpTitle').textContent = "Edit Retail Skincare Product";
  document.getElementById('mcpName').value = prod.name;
  document.getElementById('mcpPrice').value = prod.price;
  document.getElementById('mcpTag').value = prod.tag || '';
  document.getElementById('mcpDesc').value = prod.desc || '';

  document.getElementById('modalCmsProduct')?.classList.add('active');
}

function deleteCmsProduct(id) {
  if (!confirm("Are you sure you want to remove this retail product from the website shop?")) return;
  retailProducts = getStorage('rbl_products', DEFAULT_CMS_PRODUCTS);
  retailProducts = retailProducts.filter(p => p.id !== id);
  setStorage('rbl_products', retailProducts);
  renderCmsProducts();
  showAdminToast("Retail product removed from boutique.", "warning");
}

// --- CMS Form Handlers & Persistence ---
function saveCmsHeroForm() {
  siteContent = getStorage('rbl_site_content', DEFAULT_SITE_CONTENT);
  siteContent.heroTag = getVal('cmsHeroTag', siteContent.heroTag);
  siteContent.heroTitle = getVal('cmsHeroTitle', siteContent.heroTitle);
  siteContent.heroDesc = getVal('cmsHeroDesc', siteContent.heroDesc);
  siteContent.heroRatingVal = getVal('cmsHeroRatingVal', siteContent.heroRatingVal);
  siteContent.heroRatingSub = getVal('cmsHeroRatingSub', siteContent.heroRatingSub);
  siteContent.heroPromoBadgeTitle = getVal('cmsHeroPromoBadgeTitle', siteContent.heroPromoBadgeTitle);
  siteContent.heroPromoBadgeSub = getVal('cmsHeroPromoBadgeSub', siteContent.heroPromoBadgeSub);
  siteContent.feat1Title = getVal('cmsFeat1Title', siteContent.feat1Title);
  siteContent.feat1Sub = getVal('cmsFeat1Sub', siteContent.feat1Sub);
  siteContent.feat2Title = getVal('cmsFeat2Title', siteContent.feat2Title);
  siteContent.feat2Sub = getVal('cmsFeat2Sub', siteContent.feat2Sub);
  siteContent.feat3Title = getVal('cmsFeat3Title', siteContent.feat3Title);
  siteContent.feat3Sub = getVal('cmsFeat3Sub', siteContent.feat3Sub);

  setStorage('rbl_site_content', siteContent);
  saveAutoSnapshot("CMS Update: Hero Section");
  showAdminToast("Hero section & badges saved! Live on website.", "success");
}

function saveCmsAboutForm() {
  siteContent = getStorage('rbl_site_content', DEFAULT_SITE_CONTENT);
  siteContent.aboutSubtitle = getVal('cmsAboutSubtitle', siteContent.aboutSubtitle);
  siteContent.aboutTitle = getVal('cmsAboutTitle', siteContent.aboutTitle);
  siteContent.aboutYears = getVal('cmsAboutYears', siteContent.aboutYears);
  siteContent.aboutYearsText = getVal('cmsAboutYearsText', siteContent.aboutYearsText);
  siteContent.aboutPara1 = getVal('cmsAboutPara1', siteContent.aboutPara1);
  siteContent.aboutPara2 = getVal('cmsAboutPara2', siteContent.aboutPara2);
  siteContent.pillar1Title = getVal('cmsPillar1Title', siteContent.pillar1Title);
  siteContent.pillar1Desc = getVal('cmsPillar1Desc', siteContent.pillar1Desc);
  siteContent.pillar2Title = getVal('cmsPillar2Title', siteContent.pillar2Title);
  siteContent.pillar2Desc = getVal('cmsPillar2Desc', siteContent.pillar2Desc);
  siteContent.pillar3Title = getVal('cmsPillar3Title', siteContent.pillar3Title);
  siteContent.pillar3Desc = getVal('cmsPillar3Desc', siteContent.pillar3Desc);

  setStorage('rbl_site_content', siteContent);
  saveAutoSnapshot("CMS Update: About Story & Pillars");
  showAdminToast("About story & clinical pillars saved!", "success");
}

function saveCmsContactForm() {
  siteContent = getStorage('rbl_site_content', DEFAULT_SITE_CONTENT);
  siteContent.contactAddress = getVal('cmsContactAddress', siteContent.contactAddress);
  siteContent.contactPhone = getVal('cmsContactPhone', siteContent.contactPhone);
  siteContent.contactLandline = getVal('cmsContactLandline', siteContent.contactLandline);
  siteContent.contactEmail = getVal('cmsContactEmail', siteContent.contactEmail);
  siteContent.contactHours = getVal('cmsContactHours', siteContent.contactHours);
  siteContent.socialFb = getVal('cmsSocialFb', siteContent.socialFb);
  siteContent.socialIg = getVal('cmsSocialIg', siteContent.socialIg);
  siteContent.socialTiktok = getVal('cmsSocialTiktok', siteContent.socialTiktok);
  siteContent.socialWa = getVal('cmsSocialWa', siteContent.socialWa);

  // Sync phone to general banner setting as well
  localStorage.setItem('rbl_phone', siteContent.contactPhone);

  setStorage('rbl_site_content', siteContent);
  saveAutoSnapshot(`CMS Update: Contact & Location (${siteContent.contactAddress})`);
  showAdminToast("Contact details & social links saved!", "success");
}

function saveAllCms() {
  saveCmsHeroForm();
  saveCmsAboutForm();
  saveCmsContactForm();
  saveAutoSnapshot("CMS Update: Full Site Copy & Location");
  showAdminToast("All website content sections saved & synced live!", "success");
}

function resetCmsDefaults() {
  if (!confirm("Are you sure you want to reset all website copy, testimonials, FAQs, and retail products to official lounge defaults?\n(A safety backup snapshot will be saved so you can restore anytime).")) return;
  saveAutoSnapshot("Pre-CMS Reset Safety Point");
  siteContent = { ...DEFAULT_SITE_CONTENT };
  testimonials = [ ...DEFAULT_CMS_REVIEWS ];
  faqs = [ ...DEFAULT_CMS_FAQS ];
  retailProducts = [ ...DEFAULT_CMS_PRODUCTS ];

  setStorage('rbl_site_content', siteContent);
  setStorage('rbl_testimonials', testimonials);
  setStorage('rbl_faqs', faqs);
  setStorage('rbl_products', retailProducts);

  populateCmsFields();
  renderCmsReviews();
  renderCmsFaqs();
  renderCmsProducts();
  showAdminToast("All website content restored to defaults! (Safety snapshot saved)", "warning");
}

// Make functions globally accessible for inline onclick attributes
window.openEditCmsReview = openEditCmsReview;
window.deleteCmsReview = deleteCmsReview;
window.openEditCmsFaq = openEditCmsFaq;
window.deleteCmsFaq = deleteCmsFaq;
window.openEditCmsProduct = openEditCmsProduct;
window.deleteCmsProduct = deleteCmsProduct;
window.exportFullJsonBackup = exportFullJsonBackup;
window.restoreAutoSnapshot = restoreAutoSnapshot;
window.handleBackupFileSelect = handleBackupFileSelect;

// ==========================================================================
// 13. EVENT LISTENERS INITIALIZATION
// ==========================================================================
window.addEventListener('DOMContentLoaded', () => {
  // 1. Immediately bind login form & submit button
  const loginForm = document.getElementById('adminLoginForm');
  const doLogin = (e) => {
    if (e) e.preventDefault();
    const user = document.getElementById('adminUsername')?.value || '';
    const pass = document.getElementById('adminPassword')?.value || '';
    loginAdmin(user, pass);
  };
  loginForm?.addEventListener('submit', doLogin);
  document.getElementById('btnAdminSignIn')?.addEventListener('click', (e) => {
    const user = document.getElementById('adminUsername')?.value || '';
    const pass = document.getElementById('adminPassword')?.value || '';
    if (user && pass) {
      e.preventDefault();
      loginAdmin(user, pass);
    }
  });

  // 2. Auth check and initial render
  try { checkAuth(); } catch (err) { console.warn("checkAuth error:", err); }
  try { refreshAllPanels(); } catch (err) { console.warn("refreshAllPanels error:", err); }

  // Tab buttons
  document.querySelectorAll('.nav-tab-btn').forEach(btn => {
    btn.addEventListener('click', () => switchTab(btn.dataset.tab));
  });

  // Handle URL hash navigation (e.g. admin.html#cms)
  const hash = window.location.hash.replace('#', '');
  if (hash && document.getElementById(`pane-${hash}`)) {
    switchTab(hash);
  }

  // Filters & searches
  document.getElementById('appointmentSearch')?.addEventListener('input', renderAppointments);
  document.getElementById('appointmentStatusFilter')?.addEventListener('change', renderAppointments);
  document.getElementById('serviceSearch')?.addEventListener('input', renderServices);
  document.getElementById('serviceCategoryFilter')?.addEventListener('change', renderServices);
  document.getElementById('messageSearch')?.addEventListener('input', renderMessages);

  document.getElementById('btnLogoutAdmin')?.addEventListener('click', logoutAdmin);

  // Super Admin Security Form (Password Change)
  document.getElementById('adminSecurityForm')?.addEventListener('submit', (e) => {
    e.preventDefault();
    const newPass = document.getElementById('secAdminPassword').value.trim();
    if (!newPass || newPass.length < 6) {
      showAdminToast("Password must be at least 6 characters.", "warning");
      return;
    }
    localStorage.setItem('rbl_admin_password', newPass);
    document.getElementById('secAdminPassword').value = '';
    showAdminToast("Super Admin master password updated successfully!", "success");
  });

  // CMS Forms and Buttons
  document.getElementById('cmsHeroForm')?.addEventListener('submit', (e) => {
    e.preventDefault();
    saveCmsHeroForm();
  });

  document.getElementById('cmsAboutForm')?.addEventListener('submit', (e) => {
    e.preventDefault();
    saveCmsAboutForm();
  });

  document.getElementById('cmsContactForm')?.addEventListener('submit', (e) => {
    e.preventDefault();
    saveCmsContactForm();
  });

  document.getElementById('btnSaveCmsAll')?.addEventListener('click', saveAllCms);
  document.getElementById('btnResetCmsDefaults')?.addEventListener('click', resetCmsDefaults);

  // Data Protection, JSON Backup & Restore
  const backupFileInput = document.getElementById('importJsonFileInput');
  if (backupFileInput) {
    backupFileInput.addEventListener('change', handleBackupFileSelect);
  }
  document.getElementById('btnTriggerRestoreBackup')?.addEventListener('click', () => {
    backupFileInput?.click();
  });
  document.getElementById('btnRestoreAutoSnapshot')?.addEventListener('click', () => {
    restoreAutoSnapshot();
  });

  // CMS Reviews CRUD
  document.getElementById('btnAddCmsReview')?.addEventListener('click', () => {
    document.getElementById('mcrId').value = '';
    document.getElementById('mcrTitle').textContent = "Add New Client Testimonial";
    document.getElementById('cmsReviewForm').reset();
    document.getElementById('modalCmsReview')?.classList.add('active');
  });

  document.getElementById('cmsReviewForm')?.addEventListener('submit', (e) => {
    e.preventDefault();
    const id = document.getElementById('mcrId').value;
    const name = document.getElementById('mcrName').value.trim();
    const rating = parseInt(document.getElementById('mcrRating').value) || 5;
    const tag = document.getElementById('mcrTag').value.trim();
    const comment = document.getElementById('mcrComment').value.trim();

    testimonials = getStorage('rbl_testimonials', DEFAULT_CMS_REVIEWS);

    if (id) {
      const idx = testimonials.findIndex(r => r.id === id);
      if (idx !== -1) {
        testimonials[idx] = { ...testimonials[idx], name, rating, tag, comment };
      }
    } else {
      const initials = name.split(' ').map(n => n[0]).join('').slice(0, 2).toUpperCase() || 'CL';
      testimonials.push({
        id: 'rev-' + Date.now(),
        name,
        avatar: initials,
        tag,
        rating,
        comment
      });
    }

    setStorage('rbl_testimonials', testimonials);
    renderCmsReviews();
    closeModal('modalCmsReview');
    showAdminToast("Testimonial saved! Updated on website.", "success");
  });

  // CMS FAQs CRUD
  document.getElementById('btnAddCmsFaq')?.addEventListener('click', () => {
    document.getElementById('mcfId').value = '';
    document.getElementById('mcfTitle').textContent = "Add New FAQ Item";
    document.getElementById('cmsFaqForm').reset();
    document.getElementById('modalCmsFaq')?.classList.add('active');
  });

  document.getElementById('cmsFaqForm')?.addEventListener('submit', (e) => {
    e.preventDefault();
    const id = document.getElementById('mcfId').value;
    const question = document.getElementById('mcfQuestion').value.trim();
    const answer = document.getElementById('mcfAnswer').value.trim();

    faqs = getStorage('rbl_faqs', DEFAULT_CMS_FAQS);

    if (id) {
      const idx = faqs.findIndex(f => f.id === id);
      if (idx !== -1) {
        faqs[idx] = { ...faqs[idx], question, answer };
      }
    } else {
      faqs.push({
        id: 'faq-' + Date.now(),
        question,
        answer
      });
    }

    setStorage('rbl_faqs', faqs);
    renderCmsFaqs();
    closeModal('modalCmsFaq');
    showAdminToast("FAQ saved! Updated on website.", "success");
  });

  // CMS Products CRUD
  document.getElementById('btnAddCmsProduct')?.addEventListener('click', () => {
    document.getElementById('mcpId').value = '';
    document.getElementById('mcpTitle').textContent = "Add Retail Skincare Product";
    document.getElementById('cmsProductForm').reset();
    document.getElementById('modalCmsProduct')?.classList.add('active');
  });

  document.getElementById('cmsProductForm')?.addEventListener('submit', (e) => {
    e.preventDefault();
    const id = document.getElementById('mcpId').value;
    const name = document.getElementById('mcpName').value.trim();
    const price = parseFloat(document.getElementById('mcpPrice').value) || 0;
    const tag = document.getElementById('mcpTag').value.trim() || 'Boutique';
    const desc = document.getElementById('mcpDesc').value.trim();
    const priceDisplay = `₱${price.toLocaleString()}.00`;

    retailProducts = getStorage('rbl_products', DEFAULT_CMS_PRODUCTS);

    if (id) {
      const idx = retailProducts.findIndex(p => p.id === id);
      if (idx !== -1) {
        retailProducts[idx] = { ...retailProducts[idx], name, price, priceDisplay, tag, desc };
      }
    } else {
      retailProducts.push({
        id: 'prod-' + Date.now(),
        name,
        price,
        priceDisplay,
        tag,
        icon: 'fa-pump-soap',
        desc
      });
    }

    setStorage('rbl_products', retailProducts);
    renderCmsProducts();
    closeModal('modalCmsProduct');
    showAdminToast("Retail product saved! Synced with website boutique.", "success");
  });
});
