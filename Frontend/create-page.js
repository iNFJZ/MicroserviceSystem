#!/usr/bin/env node

const fs = require('fs');
const path = require('path');

// Get command line arguments
const args = process.argv.slice(2);

if (args.length < 1) {
    console.log('Usage: node create-page.js <page-name> [page-title] [menu-icon] [menu-text]');
    console.log('');
    console.log('Examples:');
    console.log('  node create-page.js settings "System Settings" ti-settings "Settings"');
    console.log('  node create-page.js reports "User Reports" ti-chart-bar "Reports"');
    console.log('  node create-page.js analytics "Analytics Dashboard" ti-chart-pie "Analytics"');
    process.exit(1);
}

const pageName = args[0];
const pageTitle = args[1] || pageName.charAt(0).toUpperCase() + pageName.slice(1);
const menuIcon = args[2] || 'ti-file';
const menuText = args[3] || pageTitle;

// Validate page name
if (!/^[a-z0-9-]+$/.test(pageName)) {
    console.error('Error: Page name must contain only lowercase letters, numbers, and hyphens');
    process.exit(1);
}

// Paths
const templatePath = path.join(__dirname, 'html/admin/template.html');
const htmlOutputPath = path.join(__dirname, 'html/admin', `${pageName}.html`);
const jsOutputPath = path.join(__dirname, 'js', `${pageName}-backend.js`);

// Check if files already exist
if (fs.existsSync(htmlOutputPath)) {
    console.error(`Error: File ${htmlOutputPath} already exists`);
    process.exit(1);
}

if (fs.existsSync(jsOutputPath)) {
    console.error(`Error: File ${jsOutputPath} already exists`);
    process.exit(1);
}

// Read template
if (!fs.existsSync(templatePath)) {
    console.error(`Error: Template file ${templatePath} not found`);
    process.exit(1);
}

let templateContent = fs.readFileSync(templatePath, 'utf8');

// Replace placeholders
templateContent = templateContent
    .replace(/PAGE_TITLE/g, pageTitle)
    .replace(/PAGE_NAME/g, pageName)
    .replace(/MENU_ICON/g, menuIcon)
    .replace(/MENU_TEXT/g, menuText);

// Create HTML file
fs.writeFileSync(htmlOutputPath, templateContent);
console.log(`✅ Created HTML file: ${htmlOutputPath}`);

// Create JavaScript file
const jsTemplate = `// ${pageName}-backend.js
$(function () {
    // Initialize page
    console.log('${pageTitle} page loaded');
    
    // Load page data
    loadPageData();
});

async function loadPageData() {
    try {
        const token = window.adminAuth.getAuthToken();
        const response = await fetch('http://localhost:5050/api/your-endpoint', {
            headers: {
                'Authorization': \`Bearer \${token}\`,
                'Content-Type': 'application/json'
            }
        });
        
        if (response.ok) {
            const data = await response.json();
            // Handle your data here
            console.log('Data loaded:', data);
        }
    } catch (error) {
        console.error('Error loading data:', error);
        if (typeof toastr !== 'undefined') {
            toastr.error('Failed to load data');
        }
    }
}

// Add your page-specific functions here
function handlePageAction() {
    // Your page logic here
    console.log('Page action triggered');
}

// Example: Handle form submission
$(document).on('submit', '#your-form', function(e) {
    e.preventDefault();
    // Handle form submission
    console.log('Form submitted');
});

// Example: Handle button clicks
$(document).on('click', '#your-button', function() {
    handlePageAction();
});
`;

fs.writeFileSync(jsOutputPath, jsTemplate);
console.log(`✅ Created JavaScript file: ${jsOutputPath}`);

// Success message
console.log('');
console.log('🎉 Page created successfully!');
console.log('');
console.log('📁 Files created:');
console.log(`   HTML: html/admin/${pageName}.html`);
console.log(`   JS: js/${pageName}-backend.js`);
console.log('');
console.log('🌐 Access your page at:');
console.log(`   http://localhost:8080/admin/${pageName}.html`);
console.log(`   http://localhost:8080/${pageName}.html`);
console.log('');
console.log('📝 Next steps:');
console.log('   1. Customize the HTML content in the card-body section');
console.log('   2. Update the JavaScript to load your specific data');
console.log('   3. Add your API endpoints');
console.log('   4. Test the page functionality');
console.log('');
console.log('💡 Tip: You can copy menu items from other pages to add navigation links'); 