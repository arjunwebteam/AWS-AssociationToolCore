/*
 * DynamicCustomFields
 * Handles conditional show/hide/enable/disable/require/optional/updateValue logic
 * for a block of custom fields rendered by the DynamicCustomFields ViewComponent.
 *
 * Container markup contract (see Default.cshtml):
 *   <div class="dynamic-custom-fields" data-conditions='[...]'>
 *     <div class="dcf-field" data-field-id="123" data-field-name="SevaType"
 *          data-field-type="Phone"
 *          data-required="true" data-required-message="This field is required.">
 *       <... class="dcf-input" ...>
 *       <span class="dcf-error"></span>
 *     </div>
 *   </div>
 *
 * NOTE: data-field-type="@field.FieldType" must be added to the .dcf-field wrapper
 * in Default.cshtml for applyPhoneFormatting() below to find Phone-type fields.
 * No other server-side change is required.
 */
var DynamicCustomFields = (function () {

    /**
     * Generic client-side formatting + validation-readiness for every
     * Phone-type custom field in the given container, on ANY form.
     * - Restricts input to digits only
     * - Auto-formats as xxx-xxx-xxxx while typing
     * - Sets a `pattern` attribute so the existing validate() function
     *   (which already reads input.pattern) enforces the format on submit,
     *   even when the admin has left "Validation Pattern" blank.
     */
    function applyPhoneFormatting(container) {
        container.querySelectorAll('.dcf-field[data-field-type="Phone"] .dcf-input').forEach(function (input) {
            input.setAttribute('maxlength', '12');
            input.setAttribute('pattern', '^\\d{3}-\\d{3}-\\d{4}$');

            input.addEventListener('input', function () {
                var ph = input.value.replace(/\D/g, '').slice(0, 10);
                if (ph.length > 3 && ph.length <= 6) {
                    ph = ph.slice(0, 3) + '-' + ph.slice(3);
                } else if (ph.length > 6) {
                    ph = ph.slice(0, 3) + '-' + ph.slice(3, 6) + '-' + ph.slice(6, 10);
                }
                input.value = ph;
            });

            input.addEventListener('keypress', function (e) {
                var charCode = e.which || e.keyCode;
                var allowed = [8, 9, 32, 46, 36, 35, 37, 39]; // backspace, tab, space, delete, home, end, left, right
                if ((charCode >= 48 && charCode <= 57) || allowed.indexOf(charCode) !== -1) return true;
                e.preventDefault();
                return false;
            });

            // Check the pattern immediately on blur, instead of waiting for final
            // form submit. jQuery Validate has no built-in support for the native
            // `pattern` attribute (only required/minlength/maxlength are auto-read),
            // so without this, the format error only ever showed up once every
            // OTHER required field on the form was already filled in too.
            input.addEventListener('blur', function () {
                var fieldWrapper = input.closest('.dcf-field');
                if (!fieldWrapper) return;
                var errorEl = fieldWrapper.querySelector('.dcf-error');
                if (!errorEl) return;

                var value = input.value || '';
                if (value && input.pattern) {
                    try {
                        if (!(new RegExp(input.pattern)).test(value)) {
                            errorEl.textContent = 'Please enter a valid phone number.';
                            fieldWrapper.classList.add('has-error');
                            return;
                        }
                    } catch (e) { /* ignore bad pattern */ }
                }
                // Only clear a pattern-type message here; leave a "required" message
                // (set elsewhere) alone if the field is simply still empty.
                if (value) {
                    errorEl.textContent = '';
                    fieldWrapper.classList.remove('has-error');
                }
            });
        });
    }

    function init(container) {
        if (!container) return;

        applyPhoneFormatting(container); // runs for every form that uses DynamicCustomFields

        var conditions = JSON.parse(container.getAttribute('data-conditions') || '[]');
        if (conditions.length === 0) return;

        // Map fieldId -> field wrapper element, and collect the set of source fieldIds to watch
        var fieldEls = {};
        container.querySelectorAll('.dcf-field').forEach(function (el) {
            fieldEls[el.getAttribute('data-field-id')] = el;
        });

        var sourceIds = [...new Set(conditions.map(function (c) { return String(c.sourceFieldId); }))];

        function getFieldValue(fieldId) {
            var el = fieldEls[fieldId];
            if (!el) return '';
            var checked = el.querySelectorAll('input[type=checkbox]:checked, input[type=radio]:checked');
            if (checked.length > 0) {
                return Array.from(checked).map(function (c) { return c.value; }).join(',');
            }
            var input = el.querySelector('.dcf-input, select, textarea, input');
            if (!input) return '';
            if (input.multiple) {
                return Array.from(input.selectedOptions).map(function (o) { return o.value; }).join(',');
            }
            return input.value || '';
        }

        function setFieldValue(fieldId, value) {
            var el = fieldEls[fieldId];
            if (!el) return;
            var input = el.querySelector('.dcf-input, select, textarea, input');
            if (input) input.value = value;
        }

        function evaluate(operatorName, actualValue, comparisonValue) {
            actualValue = (actualValue ?? '').toString();
            comparisonValue = (comparisonValue ?? '').toString();
            switch (operatorName) {
                case 'Equals': return actualValue === comparisonValue;
                case 'NotEquals': return actualValue !== comparisonValue;
                case 'Contains': return actualValue.indexOf(comparisonValue) !== -1;
                case 'NotContains': return actualValue.indexOf(comparisonValue) === -1;
                case 'GreaterThan': return parseFloat(actualValue) > parseFloat(comparisonValue);
                case 'LessThan': return parseFloat(actualValue) < parseFloat(comparisonValue);
                case 'IsEmpty': return actualValue.trim() === '';
                case 'IsNotEmpty': return actualValue.trim() !== '';
                default: return false;
            }
        }

        function setRequired(el, isRequired) {
            var checkboxes = el.querySelectorAll('input[type=checkbox]');
            if (checkboxes.length > 0) {
                // Checkbox groups: required-ness lives on the field wrapper (data-required),
                // not on any single checkbox input. wireCheckboxGroupRules() below watches
                // this attribute via MutationObserver and keeps the real jQuery Validate
                // rule in sync whenever a Require/Optional condition flips it.
                el.setAttribute('data-required', isRequired ? 'true' : 'false');
            } else {
                var input = el.querySelector('.dcf-input, select, textarea, input');
                if (!input) return;
                if (isRequired) input.setAttribute('required', 'required');
                else input.removeAttribute('required');
            }

            var requiredClass = container.getAttribute('data-required-class') || 'text-danger';
            var label = el.querySelector('label, legend');
            if (label) {
                var hasMark = label.innerHTML.indexOf(requiredClass) !== -1;
                if (isRequired && !hasMark) label.innerHTML += ' <span class="' + requiredClass + '">*</span>';
                if (!isRequired && hasMark) label.innerHTML = label.innerHTML.replace(new RegExp('\\s*<span class="' + requiredClass + '">\\*<\\/span>'), '');
            }
        }

        function setEnabled(el, isEnabled) {
            el.querySelectorAll('.dcf-input, select, textarea, input').forEach(function (i) {
                i.disabled = !isEnabled;
            });
        }

        function setVisible(el, isVisible) {
            el.style.display = isVisible ? '' : 'none';
        }

        function applyConditions() {
            conditions.forEach(function (c) {
                var actual = getFieldValue(c.sourceFieldId);
                var matched = evaluate(c.operator, actual, c.comparisonValue);
                var targetEl = fieldEls[c.targetFieldId];
                if (!targetEl) return;

                switch (c.actionType) {
                    case 'Show': setVisible(targetEl, matched); break;
                    case 'Hide': setVisible(targetEl, !matched); break;
                    case 'Enable': setEnabled(targetEl, matched); break;
                    case 'Disable': setEnabled(targetEl, !matched); break;
                    case 'Require': if (matched) setRequired(targetEl, true); break;
                    case 'Optional': if (matched) setRequired(targetEl, false); break;
                    case 'UpdateValue': if (matched) setFieldValue(c.targetFieldId, c.updatedValue); break;
                }
            });
        }

        // Re-evaluate whenever any watched source field changes
        sourceIds.forEach(function (id) {
            var el = fieldEls[id];
            if (!el) return;
            el.addEventListener('change', applyConditions);
            el.addEventListener('keyup', applyConditions);
        });

        applyConditions(); // initial pass, e.g. when editing an existing record
    }

    /**
     * Client-side mirror of BLL.CustomFields.ValidateAndSaveValues — for UX only,
     * the server call is still authoritative. Call before submitting the parent form.
     * Returns true if valid; otherwise shows inline errors and returns false.
     *
     * NOTE: checkbox-group "required" is intentionally NOT checked here anymore —
     * it is now a real jQuery Validate rule (see wireCheckboxGroupRules), enforced
     * in the same pass as every other required field. This function now covers
     * minlength/maxlength/pattern on non-checkbox fields, which jQuery Validate
     * doesn't fully auto-read.
     */
    function validate(container) {
        var valid = true;
        container.querySelectorAll('.dcf-field').forEach(function (el) {
            if (el.style.display === 'none') return; // hidden fields aren't validated
            var checkboxes = el.querySelectorAll('input[type=checkbox]');
            if (checkboxes.length > 0) return; // handled by wireCheckboxGroupRules via jQuery Validate

            var errorEl = el.querySelector('.dcf-error');
            var message = '';

            var input = el.querySelector('.dcf-input, select, textarea, input');
            if (!input) return;

            var value = input.value || '';
            var isRequired = input.hasAttribute('required');

            if (isRequired && value.trim() === '') {
                message = el.getAttribute('data-required-message') || 'This field is required.';
            } else if (value && input.minLength > 0 && value.length < input.minLength) {
                message = 'Minimum length is ' + input.minLength + '.';
            } else if (value && input.maxLength > 0 && value.length > input.maxLength) {
                message = 'Maximum length is ' + input.maxLength + '.';
            } else if (value && input.pattern) {
                try { if (!(new RegExp(input.pattern)).test(value)) message = 'Invalid format.'; } catch (e) { /* ignore bad pattern */ }
            }

            if (errorEl) errorEl.textContent = message;
            el.classList.toggle('has-error', !!message);
            if (message) valid = false;
        });
        return valid;
    }

    /**
     * Registers a real jQuery Validate "required" rule for each required
     * checkbox-group custom field, instead of relying on a manual post-check
     * inside the submitHandler. jQuery Validate's built-in required check
     * already treats same-name checkboxes as a group (it counts how many
     * elements sharing that `name` are :checked) — the same mechanism
     * already used for the static VolunteerCategoryId field in
     * AddVolunteer.cshtml ("VolunteerCategoryId": "required").
     *
     * Registering the rule this way means checkbox-group errors show up in
     * the SAME validation pass as every other required field (triggered by
     * $form.valid() on submit), instead of only appearing once every other
     * field on the form has already passed.
     */
    function wireCheckboxGroupRules(container) {
        container.querySelectorAll('.dcf-field').forEach(function (el) {
            var checkboxes = el.querySelectorAll('input[type=checkbox]');
            if (checkboxes.length === 0) return;

            var $first = $(checkboxes[0]);

            function sync() {
                var isRequired = el.getAttribute('data-required') === 'true';
                if (isRequired) {
                    $first.rules('add', {
                        required: true,
                        messages: {
                            required: el.getAttribute('data-required-message') || 'Please select at least one option.'
                        }
                    });
                } else {
                    $first.rules('remove', 'required');
                }
            }

            sync();

            // Keep the jQuery Validate rule in sync if a Require/Optional
            // condition later flips data-required on this field (see setRequired() above).
            new MutationObserver(sync).observe(el, { attributes: true, attributeFilter: ['data-required'] });
        });
    }

    /**
     * Hooks the custom-fields validation into a form that already has jQuery Validate
     * initialized (i.e. the existing `$('#SignupForm').validate({...})` / `$('#VolunteerForm').validate({...})`
     * calls already in AddVolunteer.cshtml / EditVolunteer.cshtml). Call this AFTER
     * that existing .validate({...}) call has run, so it wraps whatever submitHandler
     * (or default submit) is already configured rather than replacing the site's
     * validation setup with a second, competing one.
     *
     *   $(function () {
     *       $('#SignupForm').validate({ ...unchanged... });
     *       DynamicCustomFields.wireValidation('#SignupForm', '.dynamic-custom-fields');
     *   });
     */
    function wireValidation(formSelector, containerSelector) {
        var $form = $(formSelector);
        var validatorInstance = $form.data('validator'); // jQuery Validate stores itself here after .validate()
        if (!validatorInstance) return;

        var container = document.querySelector(containerSelector);

        // Checkbox-group "required" fields: wire as real jQuery Validate rules
        // so they validate together with every other field, not after the fact.
        if (container) {
            wireCheckboxGroupRules(container);
        }

        var originalSubmitHandler = validatorInstance.settings.submitHandler;
        validatorInstance.settings.submitHandler = function (form, event) {
            // By the time this runs, jQuery Validate has already confirmed every
            // required field AND every required checkbox group passed (see
            // wireCheckboxGroupRules). This manual pass only covers
            // minlength/maxlength/pattern on non-checkbox custom fields.
            if (container && !validate(container)) {
                return false; // block submit; per-field messages are already shown by validate()
            }
            if (originalSubmitHandler) {
                originalSubmitHandler(form, event);
            } else {
                form.submit();
            }
        };
    }

    return {
        init: init,
        validate: validate,
        wireCheckboxGroupRules: wireCheckboxGroupRules,
        wireValidation: wireValidation
    };
})();
